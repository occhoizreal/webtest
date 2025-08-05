<template>
    <div class="game-wrapper">
        <h1 class="game-title">Flappy Plane</h1>
        <div class="game-container">
            <div class="score-display">Score: {{ score }}</div>
            <canvas id="board" ref="gameBoard"></canvas>
            <div v-if="gameOver" class="game-over-overlay">
                <div class="game-over-content">
                    <h2>Allahu Akbar!</h2>
                    <p>Final Score: {{ score }}</p>
                    <button @click="resetGame" class="restart-btn">Play Again</button>
                </div>
            </div>
            <div v-if="!gameStarted && !gameOver" class="start-overlay">
                <div class="start-content">
                    <h2>Ready to Fly?</h2>
                    <p>Click or press any key to start</p>
                </div>
            </div>
        </div>
        <div class="controls-info">
            <p>🖱️ Click or ⌨️ Press any key to fly</p>
        </div>
    </div>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue';
import flappyplane from '../image/flappyplane.png'
import toppipe from '../image/toppipe.png'
import bottompipe from '../image/bottompipe.png'

// Reactive data
const score = ref(0);
const gameOver = ref(false);
const gameStarted = ref(false);
const gameBoard = ref(null);

// Game variables
let board;
let boardWidth = 480;
let boardHeight = 720;
let context;

// Plane dimensions (scaled from 814x228)
let planeWidth = 100;
let planeHeight = 28;
let planeX = boardWidth / 8;
let planeY = boardHeight / 2;
let planeImg;

let plane = {
    x: planeX,
    y: planeY,
    width: planeWidth,
    height: planeHeight
}

// Tower/Pipe dimensions (scaled from 384x2312)
let towerArray = [];
let towerWidth = 80;
let towerHeight = 480;
let towerX = boardWidth;
let towerY = 0;

let topPipeImg;
let bottomPipeImg;

// Physics
let velocityX = -2;
let velocityY = 0;
let gravity = 0.1;
let jump = -3;

let animationId;
let pipeInterval;

onMounted(() => {
    initGame();
});

onUnmounted(() => {
    if (animationId) {
        cancelAnimationFrame(animationId);
    }
    if (pipeInterval) {
        clearInterval(pipeInterval);
    }
});

function initGame() {
    board = gameBoard.value;
    board.height = boardHeight;
    board.width = boardWidth;
    context = board.getContext("2d");

    // Load images
    planeImg = new Image();
    planeImg.src = flappyplane;
    planeImg.onload = function() {
        drawInitialState();
    }

    topPipeImg = new Image();
    topPipeImg.src = toppipe;
    
    bottomPipeImg = new Image();
    bottomPipeImg.src = bottompipe;

    // Event listeners
    document.addEventListener("mousedown", handleInput);
    document.addEventListener("keydown", handleInput);
}

function drawInitialState() {
    context.clearRect(0, 0, board.width, board.height);
    // Background is handled by CSS background-image
    
    // Draw plane
    context.drawImage(planeImg, plane.x, plane.y, plane.width, plane.height);
}

function startGame() {
    if (!gameStarted.value) {
        gameStarted.value = true;
        update();
        pipeInterval = setInterval(placePipes, 1300);
    }
}

function update() {
    animationId = requestAnimationFrame(update);
    
    if (gameOver.value) {
        return;
    }

    context.clearRect(0, 0, board.width, board.height);
    // Background is handled by CSS background-image

    // Update plane physics
    velocityY += gravity;
    plane.y += velocityY;
    
    // Check ground/ceiling collision
    if (plane.y <= 0 || plane.y + plane.height >= boardHeight) {
        gameOver.value = true;
        return;
    }
    
    // Draw plane
    context.save();
    context.translate(plane.x + plane.width/2, plane.y + plane.height/2);
    context.rotate(Math.min(velocityY * 0.05, 0.5));
    context.drawImage(planeImg, -plane.width/2, -plane.height/2, plane.width, plane.height);
    context.restore();

    // Update and draw pipes
    for (let i = 0; i < towerArray.length; i++) {
        let pipe = towerArray[i];
        pipe.x += velocityX;
        context.drawImage(pipe.img, pipe.x, pipe.y, pipe.width, pipe.height);

        if (!pipe.passed && plane.x > pipe.x + pipe.width) {
            score.value += 0.5;
            pipe.passed = true;
        }
        
        if (detectCollision(plane, pipe)) {
            gameOver.value = true;
        }
    }

    // Remove off-screen pipes
    while (towerArray.length > 0 && towerArray[0].x < -towerWidth) {
        towerArray.shift();
    }
}

function placePipes() {
    if(gameOver.value || !gameStarted.value){
        return;
    }
    if(towerArray.length > 0){
        let lastPipe = towerArray[towerArray.length - 1];
        if (lastPipe.x > boardWidth - 200) { // Minimum 200px spacing
            return;
        }
    }
    
    let randomPipeY = towerY - towerHeight/4 - Math.random() * (towerHeight/2);
    let openingSpace = boardHeight/5.5;
    
    let topPipe = {
        img: topPipeImg,
        x: towerX,
        y: randomPipeY,
        width: towerWidth,
        height: towerHeight,
        passed: false
    };
    
    towerArray.push(topPipe);

    let bottomPipe = {
        img: bottomPipeImg,
        x: towerX,
        y: randomPipeY + towerHeight + openingSpace,
        width: towerWidth,
        height: towerHeight,
        passed: false
    };
    
    towerArray.push(bottomPipe);
}

function handleInput(e) {
    e.preventDefault();
    
    if (!gameStarted.value && !gameOver.value) {
        startGame();
    }
    
    if (!gameOver.value && gameStarted.value) {
        velocityY = jump;
        
        // Handle 'X' key specifically like in original
        if (e.code === 'KeyX') {
            velocityY = jump;
        }
    }
}

function detectCollision(a, b) {
    return a.x < b.x + b.width && 
           a.x + a.width > b.x && 
           a.y < b.y + b.height && 
           a.y + a.height > b.y;
}

function resetGame() {
    if (pipeInterval) {
        clearInterval(pipeInterval);
        pipeInterval = null;
    }
    // Reset game state
    gameOver.value = false;
    gameStarted.value = false;
    score.value = 0;
    
    // Reset plane position
    plane.x = planeX;
    plane.y = planeY;
    velocityY = 0;
    
    // Clear pipes
    towerArray = [];
    
    // Cancel animation
    if (animationId) {
        cancelAnimationFrame(animationId);
    }
    
    // Redraw initial state
    drawInitialState();
}
</script>

<style scoped>
@import url('../styles/game.css');
</style>