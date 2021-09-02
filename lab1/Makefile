.PHONY: build check clean deploy

help :
	@echo "Usage:"
	@echo "   make build               - build the plug-in"
	@echo "   make check               - check the endpoints with curl"
	@echo "   make clean               - delete the istio plugin from ngsa"
	@echo "   make deploy              - deploy the istio plugin to ngsa"

build :
	# build the WebAssembly
	@rm -f burst_header.wasm
	@cargo build --release --target=wasm32-unknown-unknown
	@cp target/wasm32-unknown-unknown/release/burst_header.wasm .

deploy : clean build

	# add config map
	@kubectl create cm burst-wasm-filter --from-file=burst_header.wasm

	# patch deployment
	@# this will create a new deployment and terminate the old one
	@kubectl patch deployment ngsa -p '{"spec":{"template":{"metadata":{"annotations":{"sidecar.istio.io/userVolume":"[{\"name\":\"wasmfilters-dir\",\"configMap\": {\"name\": \"burst-wasm-filter\"}}]","sidecar.istio.io/userVolumeMount":"[{\"mountPath\":\"/var/local/lib/wasm-filters\",\"name\":\"wasmfilters-dir\"}]"}}}}}'

	# turn the wasm filter on
	@kubectl apply -f ../deploy/filter.yaml

	# wait for pods to be ready
	@kubectl get pods

check :
	@# check the healthz endpoint
	@http http://localhost:30080/healthz

clean :
	# delete filter and config map
	@kubectl patch deployment ngsa -p '{"spec":{"template":{"metadata":{"annotations":{"sidecar.istio.io/userVolume":"[]","sidecar.istio.io/userVolumeMount":"[]"}}}}}'
	@kubectl delete --ignore-not-found -f ../deploy/filter.yaml
	@kubectl delete --ignore-not-found cm burst-wasm-filter
