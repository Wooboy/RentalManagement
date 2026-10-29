<template>
  <div class="card bg-base-100 shadow p-4">
    <div class="flex flex-wrap items-end gap-3 mb-3">
      <label class="form-control min-w-44">
        <span class="label-text mb-1">合約</span>
        <select v-model.number="contractFilter" class="select select-bordered select-sm" @change="load">
          <option :value="0">全部合約</option>
          <option v-for="c in contracts" :key="c.id" :value="c.id">{{ c.contractName || c.contractNo }}</option>
        </select>
      </label>
      <label class="form-control min-w-36">
        <span class="label-text mb-1">繳費狀態</span>
        <select v-model="paidFilter" class="select select-bordered select-sm" @change="load">
          <option value="all">全部</option>
          <option value="unpaid">未繳</option>
          <option value="paid">已繳</option>
        </select>
      </label>
    </div>

    <p v-if="error" class="text-error text-sm mb-2">{{ error }}</p>
    <p v-if="!loading && items.length === 0" class="text-sm text-base-content/60 py-8 text-center">沒有符合條件的帳單</p>

    <div class="overflow-x-auto">
      <table v-if="items.length > 0" class="table table-zebra table-sm">
        <thead>
          <tr>
            <th>類別</th>
            <th>帳期</th>
            <th class="text-right">金額</th>
            <th>狀態</th>
            <th>繳費日</th>
            <th></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="c in items" :key="c.id">
            <td>{{ categoryText(c.category) }}</td>
            <td class="whitespace-nowrap">{{ c.billingStartUtc?.slice(0, 10) }} ~ {{ c.billingEndUtc?.slice(0, 10) }}</td>
            <td class="text-right">${{ Number(c.amount).toLocaleString() }}</td>
            <td>
              <span class="badge badge-sm" :class="c.isPaid ? 'badge-success' : 'badge-warning'">
                {{ c.isPaid ? '已繳' : '未繳' }}
              </span>
            </td>
            <td>{{ c.paidAtUtc?.slice(0, 10) || '-' }}</td>
            <td class="text-right">
              <button class="btn btn-xs" @click="attachmentTarget = c">收據</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <AttachmentManager
      v-if="attachmentTarget"
      :entity-type="2"
      :entity-id="attachmentTarget.id"
      :subtitle="`帳單：${categoryText(attachmentTarget.category)}（${attachmentTarget.billingStartUtc?.slice(0, 10)}）`"
      base-path="/portal/attachments"
      readonly
      :can-delete="false"
      @close="attachmentTarget = null"
    />
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import api from '../../services/api'
import AttachmentManager from '../../components/AttachmentManager.vue'

const items = ref<any[]>([])
const contracts = ref<any[]>([])
const contractFilter = ref(0)
const paidFilter = ref<'all' | 'paid' | 'unpaid'>('all')
const loading = ref(true)
const error = ref('')
const attachmentTarget = ref<any>(null)

const categoryText = (v: number) => ({ 1: '租金', 2: '水費', 3: '電費' } as Record<number, string>)[v] || '其他'

const load = async () => {
  error.value = ''
  try {
    const params: any = {}
    if (contractFilter.value > 0) params.contractId = contractFilter.value
    if (paidFilter.value === 'paid') params.isPaid = true
    if (paidFilter.value === 'unpaid') params.isPaid = false
    const { data } = await api.get('/portal/charges', { params })
    items.value = data
  } catch (e: any) {
    error.value = e?.response?.data || e?.message || '載入帳單失敗'
  } finally {
    loading.value = false
  }
}

onMounted(async () => {
  try {
    const { data } = await api.get('/portal/contracts')
    contracts.value = data
  } catch { /* 列表載入失敗時仍顯示帳單 */ }
  await load()
})
</script>
