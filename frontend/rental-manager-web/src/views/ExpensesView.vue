<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">支出費用</h2>
    <button class="btn btn-primary mb-3" @click="add">新增水費支出</button>
    <ul><li v-for="e in items" :key="e.id">{{ e.category }} - {{ e.amount }}</li></ul>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';import api from '../services/api'
const items = ref<any[]>([])
const load = async()=>{ const {data}=await api.get('/expenses'); items.value=data }
onMounted(load)
const add = async()=>{ const now=new Date().toISOString(); await api.post('/expenses',{category:1,billingStartUtc:now,billingEndUtc:now,amount:500,occurredAtUtc:now}); await load() }
</script>
