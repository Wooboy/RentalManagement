<template>
  <div class="space-y-4">
    <div class="card bg-base-100 shadow p-4">
      <h2 class="text-lg font-bold mb-2">房源管理</h2>
      <div class="flex flex-wrap items-end gap-3 mb-3">
        <label class="form-control min-w-72">
          <span class="label-text mb-1">搜尋關鍵字</span>
          <input v-model="keyword" class="input input-bordered" placeholder="名稱/地址" />
        </label>
        <button class="btn" @click="loadProperties">查詢</button>
        <button class="btn btn-primary" @click="openCreatePropertyModal">新增</button>
      </div>
      <table class="table table-zebra">
        <thead><tr><th>名稱</th><th>地址</th><th></th></tr></thead>
        <tbody>
          <tr v-for="p in properties" :key="p.id" :class="selectedPropertyId===p.id ? 'bg-base-200' : ''">
            <td>{{ p.name }}</td><td>{{ p.address }}</td>
            <td class="flex gap-2 justify-end">
              <button class="btn btn-sm" @click="selectProperty(p.id)">房間</button>
              <button class="btn btn-sm" @click="editProperty(p)">編輯</button>
              <button class="btn btn-sm btn-error" @click="removeProperty(p.id)">刪除</button>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <div class="card bg-base-100 shadow p-4">
      <h2 class="text-lg font-bold mb-2">房間管理</h2>
      <div class="flex items-end gap-2 mb-3">
        <p class="text-sm flex-1">目前房源：{{ currentPropertyLabel }}</p>
        <button class="btn btn-primary" :disabled="!selectedPropertyId" @click="openCreateRoomModal">新增</button>
      </div>
      <table class="table table-zebra">
        <thead><tr><th>房間名稱</th><th>備註</th><th></th></tr></thead>
        <tbody>
          <tr v-for="r in rooms" :key="r.id">
            <td>{{ r.name }}</td><td>{{ r.notes }}</td>
            <td class="flex gap-2 justify-end">
              <button class="btn btn-sm" @click="editRoom(r)">編輯</button>
              <button class="btn btn-sm btn-error" @click="removeRoom(r.id)">刪除</button>
            </td>
          </tr>
        </tbody>
      </table>
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
