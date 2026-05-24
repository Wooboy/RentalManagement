<template>
  <div class="card bg-base-100 shadow p-4 max-w-xl">
    <h2 class="text-lg font-bold mb-2">電費試算（依度數）</h2>
    <div class="flex gap-2 mb-3">
      <input v-model.number="units" class="input input-bordered" placeholder="度數" type="number" />
      <input v-model.number="unitPrice" class="input input-bordered" placeholder="每度金額" type="number" />
      <button class="btn btn-primary" @click="calc">計算</button>
    </div>
    <p>應繳：{{ result }}</p>
  </div>
</template>
<script setup lang="ts">
import { ref } from 'vue';import api from '../services/api'
const units = ref(100); const unitPrice = ref(5); const result = ref(0)
const calc = async()=>{ const {data}=await api.post('/electricity/calculate',{ruleType:1,tenantUnits:units.value,unitPrice:unitPrice.value}); result.value=data.payableAmount }
</script>
