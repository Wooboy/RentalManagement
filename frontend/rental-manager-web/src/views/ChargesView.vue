<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">應收費用</h2>
    <button class="btn btn-primary mb-3" @click="add">新增租金應收</button>
    <ul><li v-for="c in items" :key="c.id">{{ c.category }} - {{ c.amount }}</li></ul>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';import api from '../services/api'
const items = ref<any[]>([])
const load = async()=>{ const {data}=await api.get('/charges'); items.value=data }
onMounted(load)
const add = async()=>{ const now=new Date().toISOString(); await api.post('/charges',{contractId:1,category:1,billingStartUtc:now,billingEndUtc:now,amount:10000,isPaid:false}); await load() }
</script>
