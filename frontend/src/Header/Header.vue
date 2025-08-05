<template>
    <header
        v-show="!is3DBlockPage || showHeader" 
        :class="[
            'w-full sticky top-0 z-50 transition-all duration-300', 
            isScrolled 
                ? 'bg-white/95 backdrop-blur-sm shadow-sm' 
                : 'bg-white/70 backdrop-blur-sm',
            is3DBlockPage && !showHeader 
                ? '-translate-y-full pointer-events-none' 
                : 'translate-y-0'
        ]"
    >
        <div class="container mx-auto">
            <nav class="navbar">
                <div class="logo-container">
                    <router-link to="/" class="logo">
                        <img src="../image/logo.png" alt="LOGO">
                    </router-link>
                </div>
                <ul :class="['nav-links', {'scrolled': isScrolled}]">
                    <li v-for="(item, index) in navLinks" :key="index" class="nav__item">
                        <router-link :to="item.path" :class="['nav-link', {'scrolled-link': isScrolled}]" active-class="active-nav-link">
                            {{ item.display }}
                        </router-link>
                    </li>
                    
                    <!-- Game Dropdown -->
                    <li class="nav__item game-dropdown-wrapper">
                        <div 
                            :class="['nav-link game-dropdown-trigger', {'scrolled-link': isScrolled}]"
                            @click="toggleGameDropdown"
                        >
                            Game
                            <ChevronDown :class="[
                                'w-4 h-4 ml-1 transition-transform duration-200',
                                showGameDropdown ? 'rotate-180' : 'rotate-0'
                            ]"/>
                        </div>
                        
                        <!-- Dropdown Menu -->
                        <div 
                            v-if="showGameDropdown"
                            class="game-dropdown-menu"
                        >
                            <router-link 
                                to="/flappy-plane" 
                                class="game-dropdown-item"
                                @click="closeGameDropdown"
                            >
                                Flappy Plane
                            </router-link>
                            <router-link 
                                to="/maze" 
                                class="game-dropdown-item"
                                @click="closeGameDropdown"
                            >
                                Maze
                            </router-link>
                        </div>
                    </li>
                </ul>

                <div :class="['auth-buttons', { 'scrolled-buttons': isScrolled }]">
                    <button :class="['btn', isScrolled ? 'btn-primary' : 'btn-outline']" @click="$router.push('/login')">
                        Log In
                    </button>
                    <button class="btn btn-primary" @click="$router.push('/register')">
                        Sign Up
                    </button>
                </div>
            </nav>
        </div>
    </header>
    
    <!-- Toggle button for 3D page - positioned outside header -->
    <button
        v-if="is3DBlockPage"
        @click="toggleHeader"
        :class="[
            'fixed top-4 right-4 z-[2200] bg-white/80 rounded-full p-2 shadow transition-all duration-300',
            showHeader ? 'rotate-180' : 'rotate-0'
        ]"
    >
        <ChevronsDown class="w-6 h-6 text-gray-700"/>
    </button>
</template>

<script>
import { ref, onMounted, onUnmounted, computed, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ChevronsDown, ChevronDown } from 'lucide-vue-next'

export default {
    name: 'Header',
    components: { ChevronsDown, ChevronDown },
    setup() {
        const isScrolled = ref(false)
        const showHeader = ref(true)
        const showGameDropdown = ref(false)
        const route = useRoute()

        const is3DBlockPage = computed(() => route.path === '/3d' || route.path === '/maze' || route.path === '/flappy-plane')

        const handleScroll = () => {
            isScrolled.value = window.scrollY > 0
        }

        const toggleGameDropdown = () => {
            showGameDropdown.value = !showGameDropdown.value
        }

        const closeGameDropdown = () => {
            showGameDropdown.value = false
        }

        // Close dropdown when clicking outside
        const handleClickOutside = (event) => {
            if (!event.target.closest('.game-dropdown-wrapper')) {
                showGameDropdown.value = false
            }
        }

        onMounted(() => {
            window.addEventListener('scroll', handleScroll)
            document.addEventListener('click', handleClickOutside)
            // Initialize header state for 3D page
            if (is3DBlockPage.value) {
                showHeader.value = false
            }
        })

        onUnmounted(() => {
            window.removeEventListener('scroll', handleScroll)
            document.removeEventListener('click', handleClickOutside)
        })

        watch(
            () => route.path,
            (newPath) => {
                if (newPath === '/3d' || newPath === '/maze' || newPath === '/flappy-plane') {
                    showHeader.value = false
                } else {
                    showHeader.value = true
                }
            }
        )

        const toggleHeader = () => {
            showHeader.value = !showHeader.value
        }

        const navLinks = [
            { path: '/', display: 'Home' },
            { path: '/about', display: 'About' },
            { path: '/chatbot', display: 'Chat Bot'},
            // { path: '/text-editor', display: 'Text Editor' },
            { path: '/3d', display: '3D Block' }
        ]

        return {
            isScrolled,
            navLinks,
            showHeader,
            showGameDropdown,
            is3DBlockPage,
            toggleHeader,
            toggleGameDropdown,
            closeGameDropdown
        }
    }
}
</script>

<style scoped>
@import url(header.css);


</style>