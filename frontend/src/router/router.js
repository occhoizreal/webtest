import Home from "@/Page/Home.vue";
import About from "@/Page/About.vue"
import FlappyPlane from "@/Page/FlappyPlane.vue"
import TextEditor from "@/Page/TextEditor.vue";
import ThreeDBlock from "@/Page/3DBlock.vue"
import Maze from "@/Page/Maze.vue"
import Services from "@/Page/Services.vue"
import Contact from "@/Page/Contact.vue"
import Login from "@/Page/Login.vue"
import Register from "@/Page/Register.vue"
import ChatBot from "@/Page/ChatBot.vue"
const routers = [
    {
        path: '/',
        name: 'Home',
        component: Home
    },
    {
        path: '/about',
        name: 'About',
        component: About
    },
    {
        path: '/flappy-plane',
        name: 'Flappy Game',
        component: FlappyPlane
    },
    {
        path:'/text-editor',
        name: 'TextEditor',
        component: TextEditor
    },
    {
        path: '/3d',
        name: '3D Block',
        component: ThreeDBlock 
    },
    {
        path: '/maze',
        name: 'Maze',
        component: Maze
    },
    {
        path: '/contact',
        name: 'Contact',
        component: Contact
    },
    {
        path: '/services',
        name: 'Services',
        component: Services
    },
    {
        path: '/login',
        name: 'Login',
        component: Login
    },
    {
        path: '/register',
        name: 'Register',
        component: Register
    },
    {
        path: '/chatbot',
        name: 'Chat Bot',
        component: ChatBot
    }
]

export default routers;