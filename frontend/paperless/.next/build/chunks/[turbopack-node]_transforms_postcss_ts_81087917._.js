module.exports = [
"[turbopack-node]/transforms/postcss.ts { CONFIG => \"[project]/frontend/paperless/postcss.config.mjs [postcss] (ecmascript)\" } [postcss] (ecmascript, async loader)", ((__turbopack_context__) => {

__turbopack_context__.v((parentImport) => {
    return Promise.all([
  "chunks/f645a_c879ba8e._.js",
  "chunks/[root-of-the-server]__b0c1de22._.js"
].map((chunk) => __turbopack_context__.l(chunk))).then(() => {
        return parentImport("[turbopack-node]/transforms/postcss.ts { CONFIG => \"[project]/frontend/paperless/postcss.config.mjs [postcss] (ecmascript)\" } [postcss] (ecmascript)");
    });
});
}),
];