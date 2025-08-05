<template>
    <div>
        <Header/>
        <router-view/>
        <Footer/>
    </div>
</template>
<script>
import {useRoute} from 'vue-router'
import {computed} from 'vue'
import Header from "@/Header/Header.vue"
import Footer from "@/Footer/Footer.vue"

export default{
    name: 'Layout',
    components: {
        Header,
        Footer
    },
    setup(){
        const route = useRoute()
        return{
            route
        }
    }
}
</script>