<template>
  <div class="relative overflow-hidden rounded-xl border border-base-300 bg-base-100 p-4 shadow-sm">
    <div class="flex items-start justify-between gap-3">
      <div class="min-w-0">
        <p class="text-xs font-medium text-base-content/55 truncate">{{ title }}</p>
        <p class="mt-1.5 text-2xl font-semibold leading-none tabular-nums" :class="valueClass">{{ value }}</p>
        <p v-if="desc" class="mt-1.5 text-xs text-base-content/45">{{ desc }}</p>
      </div>
      <div v-if="icon" class="grid place-items-center w-9 h-9 rounded-lg shrink-0" :class="accentClass">
        <svg xmlns="http://www.w3.org/2000/svg" class="w-5 h-5" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="1.8">
          <path stroke-linecap="round" stroke-linejoin="round" :d="icon" />
        </svg>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'

const props = defineProps<{
  title: string
  value: string | number
  desc?: string
  valueClass?: string
  icon?: string
  accent?: 'primary' | 'success' | 'warning' | 'error' | 'info' | 'neutral'
}>()

const accentMap: Record<string, string> = {
  primary: 'bg-emerald-500/10 text-emerald-600',
  success: 'bg-emerald-500/10 text-emerald-600',
  warning: 'bg-amber-500/10 text-amber-600',
  error: 'bg-red-500/10 text-red-600',
  info: 'bg-sky-500/10 text-sky-600',
  neutral: 'bg-slate-500/10 text-slate-600'
}

const accentClass = computed(() => accentMap[props.accent || 'primary'])
</script>
