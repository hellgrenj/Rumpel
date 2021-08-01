import { create } from "https://deno.land/x/djwt@v2.2/mod.ts";
import { getNumericDate } from "https://deno.land/x/djwt@v2.2/mod.ts";

export const sleep = (ms: number) => {
  return new Promise((resolve) => {
    setTimeout(() => {
      resolve("done");
    }, ms);
  });
};
export const waitForEndpoint = async (url: string): Promise<void> => {
  try {
    const response = await fetch(url, { method: "GET" });
    if (response.status !== 200) {
      throw new Error(`status code ${response.status}`);
    } else {
      console.log(`${url} ready, moving on..`);
      return;
    }
  } catch {
    console.log(`${url} not ready, trying again in 3 seconds..`);
    await sleep(3000);
    return waitForEndpoint(url);
  }
};
export const createJWT = async (): Promise<string> => {
  return await create(
    { alg: "HS512", typ: "JWT" },
    { exp: getNumericDate(60 * 60), foo: "bar" }, // valid for 1 hour...
    "SECRET_SYMMETRIC_KEY",
  );
};
export const getValidationResult = async (): Promise<string> => {
  const watchRumpel = Deno.run({
    cmd: [
      "docker",
      "inspect",
      "integration_rumpel_1",
    ],
    stdout: "piped",
    stdin: "piped",
    stderr: "piped",
  });
  const rumpelResult = new TextDecoder().decode(await watchRumpel.output());
  if (rumpelResult.includes('"Running": false')) {
    const validationText = Deno.run({
      cmd: [
        "docker",
        "logs",
        "integration_rumpel_1",
      ],
      stdout: "piped",
      stdin: "piped",
      stderr: "piped",
    });
    return new TextDecoder().decode(await validationText.output());
  } else {
    console.log("Rumpel validation not done yet, checking again in 3 seconds");
    await sleep(3000);
    return getValidationResult();
  }
};