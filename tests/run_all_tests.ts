import * as Colors from "https://deno.land/std@0.95.0/fmt/colors.ts";

let unitTestsPassed = false;
let integrationTestsPassed = false;
console.log(Colors.gray("running unit tests"));
const unitTests = Deno.run({
  cmd: ["dotnet", "test"],
  cwd: "./unit",
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
const unitResult = new TextDecoder().decode(await unitTests.output());
if (unitResult.includes("Passed!")) {
  unitTestsPassed = true;
}

console.log(Colors.gray("running integration tests, this can take a minute.."));
const integrationTests = Deno.run({
  cmd: ["deno", "run", "-A", "tests.ts"],
  cwd: "./integration",
  stdout: "piped",
  stdin: "piped",
  stderr: "piped",
});
const integrationResult = new TextDecoder().decode(
  await integrationTests.output(),
);
if (integrationResult.includes("integration tests passed!".toUpperCase())) {
  integrationTestsPassed = true;
}

if (unitTestsPassed && integrationTestsPassed) {
  console.log(Colors.green("✅ Both unit tests and integration tests passed!".toUpperCase()));
} else {
  if (!unitTestsPassed) {
    console.log(unitResult);
    console.log(Colors.red("❌ Unit tests failed!".toUpperCase()));
  }
  if (!integrationTestsPassed) {
    console.log(integrationResult);
    console.log(Colors.red("❌ integration tests failed!".toUpperCase()));
  }
}
