<template>
  <button
    :title="title"
    @click="$emit('click', $event)"
    @mousedown="$emit('mousedown', $event)"
    :class="[
      'p-2 rounded transition-colors',
      active
        ? (isDarkMode ? 'bg-purple-600 text-white' : 'bg-purple-500 text-white')
        : (isDarkMode ? 'hover:bg-gray-700 text-gray-400 hover:text-white' : 'hover:bg-gray-200 text-gray-600 hover:text-gray-900')
    ]"
  >
    <component :is="icon" class="w-4 h-4" />
  </button>
</template>

<script setup>
import { defineProps, inject } from 'vue'
const props = defineProps({
  icon: { type: [Object, Function], required: true },
  title: { type: String, required: true },
  active: { type: Boolean, default: false }
})
const isDarkMode = inject('isDarkMode', false) // or pass as prop
</script>