import * as Colors from "https://deno.land/std@0.95.0/fmt/colors.ts";
console.log(Colors.yellow("started to simulate consumer"));

const baseUrl = "http://localhost:8585";

const getAllCakesResponse = await fetch(`${baseUrl}/cakes`, {
  method: "GET",
});
if (getAllCakesResponse.status !== 200) {
  if (getAllCakesResponse.status === 500) {
    console.log(
      `getAllCakes received status code 500, which is OK since we simulated this with SimulatedConditions, see contract`,
    );
  } else {
    throw new Error("getAllCakes req failed");
  }
} else {
  console.log("getAllCakes req succeeded");
}

const token = Deno.env.get("BEARER_TOKEN");
const getAllSecretCakesResponse = await fetch(`${baseUrl}/scakes`, {
  method: "GET",
  headers: {
    "Authorization": "Bearer " + token,
  },
});
if (getAllSecretCakesResponse.status !== 200) {
  throw new Error("getAllSecretCakesResponse req failed");
} else {
  console.log(
    `getAllSecretCakesResponse req succeeded with Bearer token ${token}`,
  );
}

const createCakeResponse = await fetch(`${baseUrl}/cakes`, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    name: "raspberry sensation",
    ingredients: ["sugar", "love"],
  }),
});
if (createCakeResponse.status !== 201) {
  throw new Error("createCakeResponse req failed");
} else {
  console.log("createCakeResponse req succeeded");
}

const getAlLCakesResponseAgain = await fetch(`${baseUrl}/cakes`, {
  method: "GET",
});
if (getAlLCakesResponseAgain.status !== 200) {
  if (getAlLCakesResponseAgain.status === 500) {
    console.log(
      `getAlLCakesResponseAgain received status code 500, which is OK since we simulated this with SimulatedConditions, see contract`,
    );
  } else {
    throw new Error("getAlLCakesResponseAgain req failed");
  }
} else {
  console.log("getAlLCakesResponseAgain req succeeded");
}

const replaceCakeResponse = await fetch(`${baseUrl}/cakes/1`, {
  method: "PUT",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    name: "raspberry sensation2",
    ingredients: ["sugar", "love", "pineapple"],
  }),
});
if (replaceCakeResponse.status !== 200) {
  throw new Error("replaceCakeResponse request failed");
} else {
  console.log("replaceCakeResponse req succeeded");
}

const updateCakeResponse = await fetch(`${baseUrl}/cakes/1`, {
  method: "PATCH",
  headers: {
    "Content-Type": "application/json",
  },
  body: JSON.stringify({
    name: "raspberry sensation2021",
  }),
});
if (updateCakeResponse.status !== 200) {
  throw new Error("updateCakeResponse request failed");
} else {
  console.log("updateCakeResponse req succeeded");
}

const getSingleCakeByIdResponse = await fetch(`${baseUrl}/cakes/1`, {
  method: "GET",
  headers: {
    "Content-Type": "application/json",
  },
});
if (getSingleCakeByIdResponse.status !== 200) {
  throw new Error("getSingleCakeByIdResponse request failed");
} else {
  console.log("getSingleCakeByIdResponse req succeeded");
}

const getSingleCakeByQueryParamResponse = await fetch(
  `${baseUrl}/cakeByQuery?id=1`,
  {
    method: "GET",
    headers: {
      "Content-Type": "application/json",
    },
  },
);
if (getSingleCakeByQueryParamResponse.status !== 200) {
  throw new Error("getSingleCakeByQueryParamResponse request failed");
} else {
  console.log("getSingleCakeByQueryParamResponse req succeeded");
}

const deleteCakeResponse = await fetch(`${baseUrl}/cakes/1`, {
  method: "DELETE",
  headers: {
    "Content-Type": "application/json",
  },
});
if (deleteCakeResponse.status !== 200) {
  throw new Error("deleteCakeResponse request failed");
} else {
  console.log("deleteCakeResponse req succeeded");
}
console.log(Colors.yellow("consumer simulation done"));
