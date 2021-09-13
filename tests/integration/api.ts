import { Application } from "https://deno.land/x/oak@v7.7.0/mod.ts";
import { Router } from "https://deno.land/x/oak@v7.7.0/mod.ts";
import {
  create,
  getNumericDate,
  verify,
} from "https://deno.land/x/djwt@v2.3/mod.ts";

const key = await crypto.subtle.generateKey(
  { name: "HMAC", hash: "SHA-512" },
  true,
  ["sign", "verify"],
);

const getToken = async (
  { request, response }: { request: any; response: any },
) => {
  const token = await create(
    { alg: "HS512", typ: "JWT" },
    { exp: getNumericDate(60 * 60), foo: "bar" }, // valid for 1 hour...
    key,
  );
  response.body = token;
};

// API and in-memory data store of cakes..
interface ICake {
  id: number;
  name?: string;
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
      const payload = await verify(jwt, key);
      console.log(`received jwt ${JSON.stringify(payload)}`);
      const secureCake = {
        id: 1,
        name: "SecureCake",
        ingredients: "secure things",
      };
      response.status = 200;
      response.body = [secureCake];
    } catch (e) {
      console.log("failed with");
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
  console.log('V1 returning cake', cake);
  response.status = 200;
  response.body = cake;
};
const getCakeQuery = (
ctx: any,
) => {
  
  const id: number = ctx.request.url.searchParams.get('id');
  console.log('searching for cake with id ', id);
  const cake = cakes.filter((c) => c.id == id)[0];
  console.log('getCakeQuery returning cake', cake);
  if(!cake)
  {
    ctx.response.status = 404;
    ctx.response.body = {};
  } else {
    ctx.response.status = 200;
    ctx.response.body = cake;
  }
  
};
const getCakeV2 = (
  { params, response }: { params: any; response: any },
) => {
  const id: number = params.id;
  const cake = cakes.filter((c) => c.id == id)[0];
  const modifiedCake = {...cake};
  delete modifiedCake.name;
  console.log('V2 returning cake', modifiedCake);
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
router.get("/token",getToken);
router.get("/scakes", secureCakes);
router.get("/cakes", getCakes);
router.get("/cakes/:id", getCake);
router.get("/cakeByQuery", getCakeQuery);
router.get("/V2/cakes/:id", getCakeV2);
router.post("/cakes", addCake);
router.delete("/cakes/:id", deleteCake);
router.put("/cakes/:id", replaceCake);
router.patch("/cakes/:id", updateCake);

app.use(router.routes());
app.use(router.allowedMethods());
console.log(`Listening on port 1337 ...`);
await app.listen({ port: 1337 });
