<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">合約管理</h2>
    <div class="flex gap-2 mb-3"><input v-model="contractNo" class="input input-bordered" placeholder="合約編號"/><button class="btn btn-primary" @click="add">新增</button></div>
    <ul><li v-for="c in items" :key="c.id">{{ c.contractNo }} - {{ c.propertyName }}</li></ul>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';import api from '../services/api'
const items = ref<any[]>([]); const contractNo = ref('')
const load = async()=>{ const {data}=await api.get('/contracts'); items.value=data }
onMounted(load)
const add = async()=>{ await api.post('/contracts',{ contractNo:contractNo.value,tenantId:1,propertyName:'未命名房源',propertyAddress:'',startDateUtc:new Date().toISOString(),endDateUtc:new Date().toISOString(),monthlyRent:0,deposit:0,occupantCount:1,electricityRuleType:1,status:1,createdAtUtc:new Date().toISOString(),updatedAtUtc:new Date().toISOString()}); contractNo.value=''; await load() }
</script>
