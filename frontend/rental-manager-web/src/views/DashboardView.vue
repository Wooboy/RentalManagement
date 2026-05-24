<template>
  <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
    <div class="stat bg-base-100 shadow rounded-box"><div class="stat-title">收入</div><div class="stat-value">{{ report.income }}</div></div>
    <div class="stat bg-base-100 shadow rounded-box"><div class="stat-title">支出</div><div class="stat-value">{{ report.expense }}</div></div>
    <div class="stat bg-base-100 shadow rounded-box"><div class="stat-title">損益</div><div class="stat-value">{{ report.profit }}</div></div>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../services/api'
const now = new Date()
const report = ref({ income: 0, expense: 0, profit: 0 })
onMounted(async () => {
  const { data } = await api.get(`/reports/monthly?year=${now.getUTCFullYear()}&month=${now.getUTCMonth() + 1}`)
  report.value = data
})
</script>
