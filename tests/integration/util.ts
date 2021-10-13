
const sleep = (ms: number) => {
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
    await sleep(1000);
    return waitForEndpoint(url);
  }
};
