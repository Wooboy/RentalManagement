<template>
  <div class="card bg-base-100 shadow p-4">
    <h2 class="text-lg font-bold mb-2">租客管理</h2>
    <div class="flex gap-2 mb-3"><input v-model="name" class="input input-bordered" placeholder="姓名/公司"/><button class="btn btn-primary" @click="add">新增</button></div>
    <ul><li v-for="t in items" :key="t.id">{{ t.name }} ({{ t.phone }})</li></ul>
  </div>
</template>
<script setup lang="ts">
import { onMounted, ref } from 'vue';import api from '../services/api'
const items = ref<any[]>([]); const name = ref('')
const load = async()=>{ const {data}=await api.get('/tenants'); items.value=data }
onMounted(load)
const add = async()=>{ await api.post('/tenants',{ type:1,name:name.value,phone:'',createdAtUtc:new Date().toISOString(),updatedAtUtc:new Date().toISOString()}); name.value=''; await load() }
</script>
