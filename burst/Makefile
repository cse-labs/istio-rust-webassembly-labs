.PHONY: build deploy check clean

help :
	@echo "Usage:"
	@echo "   make build        - build the burst service"
	@echo "   make deploy       - build and deploy the burst service"
	@echo "   make check        - check the burst service"
	@echo "   make clean        - delete the burst service"

deploy : clean build
	# deploy apps
	@kubectl apply -f deploy

	@kubectl wait pod --for condition=ready --all --timeout=30s

build:
	# build the burst service
	@docker build . -t k3d-registry.localhost:5000/burst:local
	@docker push k3d-registry.localhost:5000/burst:local


check :
	@http http://localhost:30081/burstmetrics/default/ngsa

clean :
	# delete burst service
	@kubectl delete --ignore-not-found -f deploy/gw-burst.yaml
	@kubectl delete --ignore-not-found -f deploy/burst.yaml
