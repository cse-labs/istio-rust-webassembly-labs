#!/bin/sh

# create local registry
docker network create k3d
k3d registry create registry.localhost --port 5000
docker network connect k3d k3d-registry.localhost

# build burst metrics server
cd burst
make build
cd ..
