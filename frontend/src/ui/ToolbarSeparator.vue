<template>
  <div :class="['w-px h-6', isDarkMode ? 'bg-gray-700' : 'bg-gray-300']"></div>
</template>
<script setup>
import { inject } from 'vue'
const isDarkMode = inject('isDarkMode', false)
</script>