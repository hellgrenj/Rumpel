FROM denoland/deno:1.13.2

EXPOSE 1337

WORKDIR /app

USER deno

# Cache the dependencies as a layer (the following two steps are re-run only when deps.ts is modified).
# Ideally cache deps.ts will download and compile _all_ external files used in main.ts.
# COPY deps.ts .
# RUN deno cache deps.ts

COPY api.ts .
# Compile the main app so that it doesn't need to be compiled each startup/entry.
RUN deno cache api.ts

CMD ["run", "--allow-net", "api.ts"]