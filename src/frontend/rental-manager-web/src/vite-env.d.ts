/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 由 vite.config.ts 的 define 於建置時注入 */
  readonly APP_VERSION?: string
  readonly BUILD_TIME?: string
}
