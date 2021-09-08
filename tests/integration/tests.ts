import { getValidationResult, sleep, waitForEndpoint } from "./util.ts";
import * as Colors from "https://deno.land/std@0.95.0/fmt/colors.ts";

console.log("trying to cleanup prev run");
const stoppingRunningAPI = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-api.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stoppingRunningAPI.status();

const cleaningUpPrevRecordScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-record.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await cleaningUpPrevRecordScenario.status();
const cleaningUpPrevValidateScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-validate.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await cleaningUpPrevValidateScenario.status();
const cleaningUpPrevMockScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-mock.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await cleaningUpPrevMockScenario.status();
console.log("cleanup done, moving on..");

console.log(Colors.blue("starting test API"));

Deno.run({
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-api.yaml",
    "up",
    "--build",
  ],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});

console.log("waiting for test-api to be available..");
const apiHealthEndpoint = "http://localhost:1337/health";
await waitForEndpoint(apiHealthEndpoint);

const jwt = await fetch("http://localhost:1337/token", { method: "GET" }).then(
  async (response) => await response.text()
);

console.log(Colors.blue("starting recording scenario"));

const recordScenario = Deno.run({
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-record.yaml",
    "up",
    "--build",
  ],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});

console.log("waiting for test-api to be available..");
const apiHealthEndpointThruRumpel = "http://localhost:8585/health";
await waitForEndpoint(apiHealthEndpointThruRumpel);


const consumerSim = Deno.run({
  env: {
    "BEARER_TOKEN": jwt,
  },
  cmd: [
    "deno",
    "run",
    "-A",
    "consumer.ts",
  ],
});
await consumerSim.status();

recordScenario.close();
console.log(Colors.blue("stopping recording scenario.."));
const stoppingRecordScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-record.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stoppingRecordScenario.status();

console.log(Colors.blue("starting validation scenario.."));
Deno.run({
  env: {
    "BEARER_TOKEN": jwt,
  },
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-validate.yaml",
    "up",
    "--build",
  ],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});

await sleep(10000);

console.log("waiting for Rumpel to be done..");
const validationResult = await getValidationResult();
console.log("Rumpel validation is done!");

const validationSucceeded = validationResult.includes(
  "CONTRACT TEST PASSED! (CONTRACT: CONSUMER-API)",
);
if (validationSucceeded) {
  console.log(Colors.green("validation succeeded"));
}
console.log(Colors.blue("stopping validation scenario.."));
const stoppingValidationScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-validate.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stoppingValidationScenario.status();

console.log(Colors.blue("starting mocking scenario.."));
const mockProviderScenario = Deno.run({
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-mock.yaml",
    "up",
    "--build",
  ],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
const mockedHealthEndpoint = "http://localhost:8585/health";
await waitForEndpoint(mockedHealthEndpoint);

const consumerAgainstMockServerSim = Deno.run({
  cmd: [
    "deno",
    "run",
    "-A",
    "consumer.ts",
  ],
});
const mockingSucceeded = await consumerAgainstMockServerSim.status().then((r) =>
  r.success
);
if (mockingSucceeded) {
  console.log(Colors.green("mocking succeeded"));
}
mockProviderScenario.close();
console.log(Colors.blue("stopping mocking scenario.."));
const stoppingMockScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-mock.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stoppingMockScenario.status();


const stopApiProcess = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-api.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stopApiProcess.status();

if (validationSucceeded && mockingSucceeded) {
  console.log(
    Colors.bold(Colors.green("integration tests passed!".toUpperCase())),
  );
} else {
  console.log(
    Colors.bold(Colors.red("integration tests failed!".toUpperCase())),
  );
  console.log(validationResult);
  Deno.exit(1);
}
