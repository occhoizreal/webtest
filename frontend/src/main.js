

import { createApp } from 'vue'
import {createRouter, createWebHistory} from 'vue-router'
import axios from 'axios'
import App from './App.vue'
import 'bootstrap/dist/css/bootstrap.min.css'
import 'remixicon/fonts/remixicon.css'
import 'slick-carousel/slick/slick.css'
import 'slick-carousel/slick/slick-theme.css'
import routers from './router/router.js'

axios.defaults.baseURL = 'https://localhost:7218/api' 
axios.defaults.headers.common['Content-Type'] = 'application/json'

axios.interceptors.request.use(
  (config) => {
    console.log('Making request to:', config.baseURL + config.url)
    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Add response interceptor for error handling
axios.interceptors.response.use(
  (response) => {
    console.log('Response received:', response.status)
    return response
  },
  (error) => {
    console.error('API Error:', error.response?.data || error.message)
    return Promise.reject(error)
  }
)
const router = createRouter({
    history: createWebHistory(),
    routes: routers
})
const app = createApp(App)

app.config.globalProperties.$axios = axios
app.use(router)
app.mount('#app')
// createApp(App).mount('#app')
