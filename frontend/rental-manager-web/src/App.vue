<template>
  <div class="min-h-screen bg-base-200">
    <div class="navbar bg-base-100 shadow-sm px-6">
      <a class="text-xl font-bold">租屋管理系統</a>
      <div class="ml-6 flex gap-3 text-sm">
        <RouterLink to="/">儀表板</RouterLink>
        <RouterLink to="/tenants">租客</RouterLink>
        <RouterLink to="/contracts">合約</RouterLink>
        <RouterLink to="/charges">應收</RouterLink>
        <RouterLink to="/expenses">支出</RouterLink>
        <RouterLink to="/electricity">電費試算</RouterLink>
        <RouterLink to="/login">登入</RouterLink>
      </div>
    </div>
    <main class="p-6">
      <RouterView />
    </main>
  </div>
</template>
