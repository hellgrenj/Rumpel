import { waitForEndpoint } from "./util.ts";
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
const cleaningUpPrevVerifyScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-verify.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await cleaningUpPrevVerifyScenario.status();
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
  async (response) => await response.text(),
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

console.log(
  Colors.yellow(
    "\napplying customizations to request GET /cakes/1 and simulate a change in the API\n",
  ),
);
const contract = JSON.parse(
  Deno.readTextFileSync("./contracts/consumer-api.rumpel.contract.json"),
);
contract.Transactions.filter((t: any) =>
  t.Request.Path == "/cakes/1" && t.Request.Method == "GET"
).forEach((t: any) => {
  // copy so original is left for the mocker test below...
  contract.Transactions.splice(
    contract.Transactions.indexOf(t),
    0,
    JSON.parse(JSON.stringify(t)),
  );
  // create a V2 with customizations.. (response still expects property name, but IgnoreObjectProperty customization makes the verification pass anyway..)
  t.Request.Path = "/V2/cakes/1";
  t.Customizations.push({
    PropertyName: "name",
    Action: "IgnoreObjectProperty",
    Depth: 0,
  });
});
Deno.writeTextFileSync(
  "./contracts/consumer-api.rumpel.contract.json",
  JSON.stringify(contract, null, 2),
);

console.log(Colors.blue("starting verification scenario.."));
const verificationScenario = Deno.run({
  env: {
    "BEARER_TOKEN": jwt,
  },
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-verify.yaml",
    "up",
    "--build",
  ],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});

console.log("waiting for Rumpel to be done..");
const verificationResult = new TextDecoder().decode(
  await verificationScenario.output(),
);
console.log("Rumpel verification is done!");

const verificationSucceeded = verificationResult.includes(
  "CONTRACT TEST PASSED! (CONTRACT: CONSUMER-API)",
);
if (verificationSucceeded) {
  console.log(Colors.green("verification succeeded"));
}
console.log(Colors.blue("stopping verification scenario.."));
const stoppingVerificationScenario = Deno.run({
  cmd: ["docker-compose", "--file", "docker-compose-verify.yaml", "down"],
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
await stoppingVerificationScenario.status();

console.log(
  Colors.yellow(
    "\napplying simulated conditions to request GET /cakes\n",
  ),
);
contract.Transactions.filter((t: any) =>
  t.Request.Path == "/cakes" && t.Request.Method == "GET"
).forEach((t: any) => {
  t.SimulatedConditions.push({
    Type: "Sometimes500",
    Value: "67",
  });
  t.SimulatedConditions.push({
    Type: "FixedDelay",
    Value: "500",
  });
  t.SimulatedConditions.push({
    Type: "RandomDelay",
    Value: "100-4000",
  });
});
Deno.writeTextFileSync(
  "./contracts/consumer-api.rumpel.contract.json",
  JSON.stringify(contract, null, 2),
);

console.log(Colors.blue("starting mocking scenario.."));
const mockProviderScenario = Deno.run({
  cmd: [
    "docker-compose",
    "--file",
    "docker-compose-mock.yaml",
    "up",
    "--build",
  ],
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

if (verificationSucceeded && mockingSucceeded) {
  console.log(
    Colors.bold(Colors.green("integration tests passed!".toUpperCase())),
  );
} else {
  console.log(
    Colors.bold(Colors.red("integration tests failed!".toUpperCase())),
  );
  console.log(verificationResult);
  Deno.exit(1);
}
