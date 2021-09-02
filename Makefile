.PHONY: create delete check clean istio-check

help :
	@echo "Usage:"
	@echo "   make create              - create a k3d cluster"
	@echo "   make delete              - delete the k3d cluster"
	@echo "   make check               - check the endpoints with curl"
	@echo "   make clean               - delete the istio plugin from ngsa"
	@echo "   make istio-check         - check istio status and logs"

create : delete
	# build k3d cluster
	@k3d cluster create --registry-use k3d-registry.localhost:5000 --config deploy/k3d.yaml --k3s-server-arg "--no-deploy=traefik" --k3s-server-arg "--no-deploy=servicelb"

	# wait for cluster to be ready
	@kubectl wait node --for condition=ready --all --timeout=60s
	@sleep 5
	@kubectl wait pod -A --all --for condition=ready --timeout=60s

	# install istio into cluster
	@istioctl install --set profile=demo -y
	@kubectl label namespace default istio-injection=enabled
	@kubectl wait node --for condition=ready --all --timeout=60s

	# deploy apps
	@kubectl apply -f burst/deploy
	@kubectl apply -f deploy/ngsa

	# create HPA for ngsa deployment for testing
	@kubectl autoscale deployment ngsa --cpu-percent=50 --min=1 --max=2

	@kubectl wait pod --for condition=ready --all --timeout=60s

delete:
	# delete the cluster (if exists)
	@# this will fail harmlessly if the cluster does not exist
	-k3d cluster delete

check :
	@http http://localhost:30080/healthz

istio-check :
	@istioctl proxy-status
	@echo ""
	@kubectl logs -l=app=ngsa -c istio-proxy

clean :
	# delete filter and config map
	@kubectl patch deployment ngsa -p '{"spec":{"template":{"metadata":{"annotations":{"sidecar.istio.io/userVolume":"[]","sidecar.istio.io/userVolumeMount":"[]"}}}}}'
	@kubectl delete --ignore-not-found -f deploy/filter.yaml
	@kubectl delete --ignore-not-found cm burst-wasm-filter
