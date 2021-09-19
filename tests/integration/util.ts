
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
export const getVerificationResult = async (): Promise<string> => {
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
    const verificationText = Deno.run({
      cmd: [
        "docker",
        "logs",
        "integration_rumpel_1",
      ],
      stdout: "piped",
      stdin: "piped",
      stderr: "piped",
    });
    return new TextDecoder().decode(await verificationText.output());
  } else {
    console.log("Rumpel verification not done yet, checking again in 3 seconds");
    await sleep(3000);
    return getVerificationResult();
  }
};