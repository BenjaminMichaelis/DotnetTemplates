import { fileURLToPath, URL } from "node:url";

import { defineConfig } from "vite";

import createVuePlugin from "@vitejs/plugin-vue";
import { createAspNetCoreHmrPlugin } from "coalesce-vue/lib/build";
import createAutoImport from "unplugin-auto-import/vite";

import createVueComponentImporterPlugin from "unplugin-vue-components/vite";
import { CoalesceVuetifyResolver } from "coalesce-vue-vuetify3/build";
import { Vuetify3Resolver } from "unplugin-vue-components/resolvers";

export default defineConfig(async () => {
  return {
    build: {
      outDir: "wwwroot",
      rollupOptions: {
        output: {
          manualChunks(id) {
            if (id.match(/home/i)) return "index";
            if (id.match(/views/)) return "views";
            if (id.match(/vuetify/)) return "vuetify";
            if (id.match(/node_modules/)) return "vendor";
            return "index";
          },
        },
      },
    },

    plugins: [
      createVuePlugin(),

      // Integrations with UseViteDevelopmentServer from IntelliTect.Coalesce.Vue:
      createAspNetCoreHmrPlugin(),

      // Transforms usages of Vuetify and Coalesce components into treeshakable imports.
      // Vuetify3Resolver could be removed and replaced by vite-plugin-vuetify if desired.
      createVueComponentImporterPlugin({
        resolvers: [Vuetify3Resolver(), CoalesceVuetifyResolver()],
        dts: "src/types/components.d.ts",
      }),

      // Auto-import vue composition API functions,
      // and any custom composables in the src/composables directory:
      createAutoImport({
        imports: ["vue", "vue-router"],
        dirs: ["src/composables/*"],
        dts: "src/types/auto-imports.d.ts",
      }),
    ],

    resolve: {
      alias: {
        // Allow imports prefixed with "@" to be relative to the src folder.
        "@": fileURLToPath(new URL("src", import.meta.url)),
      },
    },

    test: {
      globals: true,
      environment: "jsdom",
      globalSetup: "./src/test-globals.ts",
      coverage: {
        provider: "v8",
        exclude: ["**/*.g.ts", "**/*.spec.*", "test{,s}/**"],
      },
      server: {
        deps: {
          inline: [/vuetify/],
        },
      },
    },
  };
});
