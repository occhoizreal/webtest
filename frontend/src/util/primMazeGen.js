import { writeFileSync, mkdirSync, readdirSync } from 'fs';
import path from 'path';
import { fileURLToPath } from 'url';
const directions = [
    [0, -2], // up
    [2, 0],  // right
    [0, 2],  // down
    [-2, 0]  // left
  ];
function createMaze(width, height) {
  // Ensure odd dimensions
  if (width % 2 === 0) width += 1;
  if (height % 2 === 0) height += 1;

  const maze = Array.from({ length: height }, () => Array(width).fill(1));

  const startX = 1;
  const startY = 1;
  maze[startY][startX] = 0;
  // maze[height - 1][width - 1] = 0
  if(Math.floor(Math.random() * 2) % 2 === 0){
    maze[height - 1][width - 2] = 0
  }
  else{
    maze[height - 2][width - 1] = 0
  }

  const walls = [];

  function addWalls(x, y) {
    for (const [dx, dy] of directions) {
      const nx = x + dx;
      const ny = y + dy;
      if (
        nx > 0 && ny > 0 && 
        nx < width && ny < height && 
        maze[ny][nx] === 1
      ) {
        walls.push([nx, ny, x, y]);
      }
    }
  }

  addWalls(startX, startY);

  while (walls.length > 0) {
    const randIndex = Math.floor(Math.random() * walls.length);
    const [x, y, px, py] = walls.splice(randIndex, 1)[0];

    if (maze[y][x] === 1) {
      let adjacentPaths = 0;
      for (const [dx, dy] of directions) {
        const nx = x + dx;
        const ny = y + dy;
        if (
          nx > 0 && ny > 0 && 
          nx < width && ny < height && 
          maze[ny][nx] === 0
        ) {
          adjacentPaths++;
        }
      }

      if (adjacentPaths === 1) {
        maze[(y + py) / 2][(x + px) / 2] = 0;
        maze[y][x] = 0;
        addWalls(x, y);
      }
    }
  }

  return maze;
}

const maze = createMaze(30, 30);



const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// Construct the path to ../map/maze.txt
const mapDir = path.resolve(__dirname, '../maps');
mkdirSync(mapDir, {recursive: true})

function genFileNumber(){
  const file = readdirSync(mapDir)
  const mazeNumbers = file.map(f => f.match(/^maze(\d+)\.txt$/)).filter(Boolean).map(match => parseInt(match[1], 10))
  const max = mazeNumbers.length > 0 ? Math.max(...mazeNumbers) : 0;
  return max + 1
}

function saveMaze(maze){
  const mazeString = maze.map(row => row.join('')).join("\n")
  const nextNumber = genFileNumber()
  const mazePath = path.join(mapDir, `maze${nextNumber}.txt`);
  writeFileSync(mazePath, mazeString);
  console.log(mazeString)
  console.log(`maze has been saved at maze${nextNumber}.txt`)
}

saveMaze(maze)

// // Construct the path to ../map/maze.txt Manually
// const mazePath = path.join(mapDir, 'maze.txt');

// // Ensure the folder exists
// mkdirSync(mapDir, { recursive: true });

// // Write the maze
// writeFileSync(mazePath, mazeString);

// const mazeString = maze.map(row => row.join('')).join('\n')
// console.log(mazeString);



