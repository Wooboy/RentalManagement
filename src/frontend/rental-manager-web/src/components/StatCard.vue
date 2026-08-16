<template>
  <div class="stat bg-base-100 shadow rounded-box">
    <div class="stat-title">{{ title }}</div>
    <div class="stat-value text-2xl lg:text-3xl" :class="valueClass">{{ value }}</div>
    <div v-if="desc" class="stat-desc">{{ desc }}</div>
  </div>
</template>

<script setup lang="ts">
defineProps<{
  title: string
  value: string | number
  desc?: string
  valueClass?: string
}>()
</script>
