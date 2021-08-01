import { Application } from "https://deno.land/x/oak@v7.7.0/mod.ts";
import { Router } from "https://deno.land/x/oak@v7.7.0/mod.ts";
import { verify } from "https://deno.land/x/djwt@v2.2/mod.ts";
const API_VERSION = Deno.args[0] ? Deno.args[0] : "V1";
console.log(`running API version ${API_VERSION}`);

// API and in-memory data store of cakes..
interface ICake {
  id: number;
  name: string;
  ingredients: Array<string>;
}

let nextId = 0;
const getNextId = (): number => {
  nextId++;
  return nextId;
};
let cakes = new Array<ICake>();
const secureCakes = async (
  { request, response }: { request: any; response: any },
) => {
  
  const bearerToken = request.headers.get("Authorization"); 
  const jwt = bearerToken.slice(7); // strip away the "Bearer " part
  if (!jwt) {
    response.status = 401;
  } else {
    try {
      const payload = await verify(jwt, "SECRET_SYMMETRIC_KEY", "HS512");
      console.log(`received jwt ${JSON.stringify(payload)}`);
      const secureCake = {
        id: 1,
        name: "SecureCake",
        ingredients: "secure things",
      };
      response.status = 200;
      response.body = [secureCake];
    } catch (e) {
      console.log('failed with')
      console.log(e);
      response.status = 401;
    }
  }
};
const getCakes = (
  { request, response }: { request: any; response: any },
) => {
  response.body = cakes;
};
const getCake = (
  { params, response }: { params: any; response: any },
) => {
  const id: number = params.id;
  const cake = cakes.filter((c) => c.id == id)[0];
  response.status = 200;
  response.body = cake;
};
const addCake = async (
  { request, response }: { request: any; response: any },
) => {
  const body = await request.body();
  const cake: ICake = await body.value;
  cake.id = getNextId();
  cakes.push(cake);
  response.status = 201;
  response.body = JSON.stringify(cake);
};
const deleteCake = (
  { params, response }: { params: any; response: any },
) => {
  const id: number = params.id;
  
  cakes = cakes.filter((c) => c.id != id);
  response.status = 200;
};
const replaceCake = async (
  { request, params, response }: { request: any; params: any; response: any },
) => {
  const id: number = params.id;
  const body = await request.body();
  const cake: ICake = await body.value;
  cake.id = id;
  
  cakes = cakes.filter((c) => c.id != cake.id);
  cakes.push(cake);
  response.status = 200;
  response.body = JSON.stringify(cake.id);
};
const updateCake = async (
  { request, params, response }: { request: any; params: any; response: any },
) => {
  const id: number = params.id;
  const body = await request.body();
  const changes: Record<string, any> = await body.value;
  const cake = cakes.filter((c) => c.id == id)[0];
  const c = cake as Record<string, any>;
  for (const k in changes) {
    c[k] = changes[k];
  }
  cakes = cakes.filter((c) => c.id != id);
  cakes.push(c as ICake);
  response.status = 200;
};
const app = new Application();
const router = new Router();
router.get("/health", ({ response }: { response: any }) => {
  response.status = 200;
});
router.get("/scakes", secureCakes);
router.get("/cakes", getCakes);
router.get("/cakes/:id", getCake);
router.post("/cakes", addCake);
router.delete("/cakes/:id", deleteCake);
router.put("/cakes/:id", replaceCake);
router.patch("/cakes/:id", updateCake);

app.use(router.routes());
app.use(router.allowedMethods());
console.log(`Listening on port 1337 ...`);
await app.listen({ port: 1337 });
