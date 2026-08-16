<template>
  <div class="flex flex-wrap items-end justify-between gap-3 mb-5">
    <div class="min-w-0">
      <h2 class="text-xl font-semibold tracking-tight">{{ title }}</h2>
      <p v-if="subtitle" class="mt-0.5 text-sm text-base-content/55">{{ subtitle }}</p>
    </div>
    <div v-if="$slots.actions" class="flex flex-wrap items-end gap-2">
      <slot name="actions" />
    </div>
  </div>
</template>

<script setup lang="ts">
defineProps<{ title: string; subtitle?: string }>()
</script>
