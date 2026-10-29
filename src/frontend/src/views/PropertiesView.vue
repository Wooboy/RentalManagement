<template>
  <div class="space-y-5">
    <PageHeader title="房源管理" :subtitle="`共 ${properties.length} 個房源`">
      <template #actions>
        <label class="input input-bordered input-sm flex items-center gap-2 w-52">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4 opacity-50" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="m21 21-5.197-5.197m0 0A7.5 7.5 0 1 0 5.196 5.196a7.5 7.5 0 0 0 10.607 10.607Z" /></svg>
          <input v-model="keyword" class="grow" placeholder="名稱 / 地址" @keyup.enter="loadProperties" />
        </label>
        <button class="btn btn-sm btn-ghost" @click="loadProperties">查詢</button>
        <button class="btn btn-sm btn-primary" @click="openCreatePropertyModal">
          <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
          新增房源
        </button>
      </template>
    </PageHeader>

    <div class="grid grid-cols-1 lg:grid-cols-2 gap-4 items-start">
      <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
        <div class="px-4 py-3 border-b border-base-300">
          <h3 class="font-semibold text-sm">房源清單</h3>
        </div>
        <div class="overflow-x-auto">
          <table class="table">
            <thead><tr><th>名稱</th><th>地址</th><th class="text-right">操作</th></tr></thead>
            <tbody>
              <tr v-for="p in properties" :key="p.id" class="cursor-pointer" :class="selectedPropertyId===p.id ? 'bg-emerald-500/10' : ''" @click="selectProperty(p.id)">
                <td class="font-medium">{{ p.name }}</td><td class="text-base-content/70">{{ p.address || '-' }}</td>
                <td>
                  <div class="flex gap-1 justify-end" @click.stop>
                    <button class="btn btn-xs btn-ghost" @click="selectProperty(p.id)">房間</button>
                    <button class="btn btn-xs btn-ghost" @click="editProperty(p)">編輯</button>
                    <button class="btn btn-xs btn-ghost text-error" @click="removeProperty(p.id)">刪除</button>
                  </div>
                </td>
              </tr>
              <tr v-if="properties.length === 0"><td colspan="3" class="text-center text-base-content/50 py-10">尚無房源，點右上角「新增房源」。</td></tr>
            </tbody>
          </table>
        </div>
      </div>

      <div class="card bg-base-100 border border-base-300 shadow-sm overflow-hidden">
        <div class="px-4 py-3 border-b border-base-300 flex items-center justify-between gap-2">
          <div class="min-w-0">
            <h3 class="font-semibold text-sm">房間清單</h3>
            <p class="text-xs text-base-content/50 truncate">目前房源：{{ currentPropertyLabel }}</p>
          </div>
          <button class="btn btn-sm btn-primary" :disabled="!selectedPropertyId" @click="openCreateRoomModal">
            <svg xmlns="http://www.w3.org/2000/svg" class="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" stroke-width="2"><path stroke-linecap="round" stroke-linejoin="round" d="M12 4.5v15m7.5-7.5h-15" /></svg>
            新增房間
          </button>
        </div>
        <div class="overflow-x-auto">
          <table class="table">
            <thead><tr><th>房間名稱</th><th>備註</th><th class="text-right">操作</th></tr></thead>
            <tbody>
              <tr v-for="r in rooms" :key="r.id">
                <td class="font-medium">{{ r.name }}</td><td class="text-base-content/70">{{ r.notes || '-' }}</td>
                <td>
                  <div class="flex gap-1 justify-end">
                    <button class="btn btn-xs btn-ghost" @click="editRoom(r)">編輯</button>
                    <button class="btn btn-xs btn-ghost text-error" @click="removeRoom(r.id)">刪除</button>
                  </div>
                </td>
              </tr>
              <tr v-if="rooms.length === 0"><td colspan="3" class="text-center text-base-content/50 py-10">{{ selectedPropertyId ? '此房源尚無房間。' : '請先於左側選擇房源。' }}</td></tr>
            </tbody>
          </table>
        </div>
      </div>
    </div>

    <dialog class="modal" :class="{ 'modal-open': showPropertyModal }">
      <div class="modal-box max-w-5xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ propertyForm.id ? '編輯房源' : '新增房源' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="form-control"><span class="label-text mb-1">房源名稱</span><input v-model="propertyForm.name" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">地址</span><input v-model="propertyForm.address" class="input input-bordered" /></label>
        </div>
        <div class="modal-action">
          <button class="btn" @click="closePropertyModal">取消</button>
          <button class="btn btn-primary" @click="saveProperty">{{ propertyForm.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>

    <dialog class="modal" :class="{ 'modal-open': showRoomModal }">
      <div class="modal-box max-w-5xl max-h-[85vh] overflow-y-auto">
        <h3 class="font-bold text-lg mb-3">{{ roomForm.id ? '編輯房間' : '新增房間' }}</h3>
        <div class="grid grid-cols-1 md:grid-cols-4 gap-3">
          <label class="form-control"><span class="label-text mb-1">房間名稱</span><input v-model="roomForm.name" class="input input-bordered" /></label>
          <label class="form-control"><span class="label-text mb-1">備註</span><input v-model="roomForm.notes" class="input input-bordered" /></label>
        </div>
        <div class="modal-action">
          <button class="btn" @click="closeRoomModal">取消</button>
          <button class="btn btn-primary" :disabled="!selectedPropertyId" @click="saveRoom">{{ roomForm.id ? '更新' : '新增' }}</button>
        </div>
      </div>
    </dialog>
  </div>
</template>
<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import api from '../services/api'
import PageHeader from '../components/PageHeader.vue'

const properties = ref<any[]>([])
const rooms = ref<any[]>([])
const keyword = ref('')
const selectedPropertyId = ref(0)
const showPropertyModal = ref(false)
const showRoomModal = ref(false)

const propertySeed = () => ({ id: 0, code: '', name: '', address: '', notes: '' })
const roomSeed = () => ({ id: 0, propertyUnitId: 0, code: '', name: '', notes: '' })
const propertyForm = ref<any>(propertySeed())
const roomForm = ref<any>(roomSeed())

const currentPropertyLabel = computed(() => {
  const p = properties.value.find((x:any)=>x.id===selectedPropertyId.value)
  return p ? `${p.name}` : '未選擇'
})

const loadProperties = async()=>{ const {data}=await api.get('/properties',{params:{keyword:keyword.value||undefined}}); properties.value=data }
const loadRooms = async()=>{ if (!selectedPropertyId.value) { rooms.value = []; return }; const {data}=await api.get('/rooms',{params:{propertyUnitId:selectedPropertyId.value}}); rooms.value=data }
onMounted(loadProperties)

const selectProperty = async(id:number)=>{ selectedPropertyId.value=id; resetRoom(); await loadRooms() }

const resetProperty = ()=> propertyForm.value = propertySeed()
const openCreatePropertyModal = ()=>{ resetProperty(); showPropertyModal.value = true }
const closePropertyModal = ()=>{ showPropertyModal.value = false; resetProperty() }
const editProperty = (p:any)=>{ propertyForm.value = { ...p }; showPropertyModal.value = true }
const saveProperty = async()=>{ if(propertyForm.value.id) await api.put(`/properties/${propertyForm.value.id}`, propertyForm.value); else await api.post('/properties', propertyForm.value); closePropertyModal(); await loadProperties() }
const removeProperty = async(id:number)=>{ await api.delete(`/properties/${id}`); if(selectedPropertyId.value===id){ selectedPropertyId.value=0; rooms.value=[] } await loadProperties() }

const resetRoom = ()=> roomForm.value = roomSeed()
const openCreateRoomModal = ()=>{ resetRoom(); showRoomModal.value = true }
const closeRoomModal = ()=>{ showRoomModal.value = false; resetRoom() }
const editRoom = (r:any)=>{ roomForm.value = { ...r }; showRoomModal.value = true }
const saveRoom = async()=>{
  if (!selectedPropertyId.value) return
  const payload = { ...roomForm.value, propertyUnitId: selectedPropertyId.value }
  if(roomForm.value.id) await api.put(`/rooms/${roomForm.value.id}`, payload)
  else await api.post('/rooms', payload)
  closeRoomModal(); await loadRooms()
}
const removeRoom = async(id:number)=>{ await api.delete(`/rooms/${id}`); await loadRooms() }
</script>
