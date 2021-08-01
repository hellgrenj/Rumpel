# Rumpel
## Simple, opinionated and automated consumer-driven contract testing for your JSON API's


![./img/rumpel.jpeg](./img/rumpel.jpeg)

## Install   
**Binaries**: See releases.  
**Docker**:   https://hub.docker.com/r/hellgrenj/rumpel.  
... Or build from source to target any platform dotnet supports.  

## Use


Record a consumer-driven contract against a known and reproducible state of the API and system under test (SUT). Use the created contract to validate that the API still works for this specific consumer when making changes to the API. Make sure you validate against the same SUT-state as you recorded against.  

You can also use the contract on the consumer side to mock the provider in local development. In this mode Rumpel will validate the consumer requests, making sure that the consumer upholds its end of the contract.  

### tldr
``./Rumpel --record-contract --target-api=http://localhost:8080 --contract-name=msA-msB``  
``./Rumpel --validate-contract --contract-path=./contracts/msA-msB.rumpel.contract.json``  
``./Rumpel --mock-provider --contract-path=./contracts/msA-msB.rumpel.contract.json``  
### Rumpel can do three things: 
- **Record a contract** (i.e turning your implicit contract into an explicit one).  
``./Rumpel --record-contract --target-api=http://localhost:8080 --contract-name=msA-msB``  
![./img/recording.jpg](./img/recording.jpg)   
Rumpel listens on port 8181 or what ever is set in an environment variable named **RUMPEL_PORT**  
- **Validate a contract** (i.e making sure the API still works for a specific consumer)  
``./Rumpel --validate-contract --contract-path=./contracts/msA-msB.rumpel.contract.json``  
![./img/validating.jpg](./img/validating.jpg)     
Validation mode supports bearer tokens and you can skip certain assertions with   **ignore-flags**.     
Run the --help command for more information.   
This should be a part of the Providers CI/CD pipeline, see ./tests/integration for an example on how to do this with docker-compose.  
  
- **Mock a provider/API**   
``./Rumpel --mock-provider --contract-path=./contracts/msA-msB.rumpel.contract.json``  
![./img/mocking.jpg](./img/mocking.jpg)  
This can be used in local dev environments to mock dependencies, see ./tests/integration for an example on how to do this with docker-compose.  
Rumpel listens on port 8181 or what ever is set in an environment variable named **RUMPEL_PORT**.     
In this mode Rumpel validates the requests sent by the consumer.  
### Rumpel has 5 commands:

``--record-contract`` or the shorthand ``-r``  
``--validate-contract`` or the shorthand ``-v``  
``--mock-provider`` or the shorthand ``-m``  
``--help`` or the shorthand ``-h``  
``--version``   

## Develop 

You need the following installed on your dev machinge:  
* dotnet 5.x
* docker 20.10.x
* docker-compose 1.29.x
* deno 1.11.x

### tests
There are both unit tests and integration tests. Check the ./tests folder.    
Run unit tests in ./tests/unit with ``dotnet test``   
Run integration tests in ./tests/integration with ``deno run -A tests.ts``   
Run ALL tests in ./tests with ``deno run -A run_all_tests.ts`` 



