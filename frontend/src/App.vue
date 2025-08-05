<template>
    <div id="app">
        <Layout/>
    </div>
</template>

<script>
import Layout from "@/layout/Layout.vue";
export default{
    name: 'App',
    components:{
        Layout
    }
}
</script>

<style>
@import url('App.css');
#app{
    font-family: Arial, Helvetica, sans-serif, Avenir;
    -webkit-font-smoothing: antialiased;
    -moz-osx-font-smoothing: grayscale;
}
</style>