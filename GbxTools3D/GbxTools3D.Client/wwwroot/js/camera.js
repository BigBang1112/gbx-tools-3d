import * as THREE from 'three';
import { MapControls } from './addons/MapControls.js';

let controls;
//let leftPressed = false;
//let rightPressed = false;
//let bothPressed = false;

export function create() {
    const camera = new THREE.PerspectiveCamera(80, window.innerWidth / window.innerHeight, 0.1, 1024*64);
    camera.position.y = 2;
    camera.position.z = 5;
    camera.lookAt(0, 0, 0);
    camera.matrixAutoUpdate = true;
    camera.matrixWorldAutoUpdate = true;
    return camera;
}

export function createMapControls(camera, renderer, targetX, targetY, targetZ) {
    if (controls) {
        controls.dispose();
    }

    controls = new MapControls(camera, renderer.domElement);

    controls.enableDamping = true; // an animation loop is required when either damping or auto-rotation are enabled
    controls.dampingFactor = 0.2;

    controls.minDistance = 1;
    controls.maxDistance = 2048;
    controls.zoomSpeed = 3;
    controls.maxPolarAngle = Math.PI / 2;
    controls.target.set(targetX, targetY, targetZ);

    /*renderer.domElement.addEventListener("mousedown", (event) => {
        if (event.button === 0) leftPressed = true; // Left button
        if (event.button === 2) rightPressed = true; // Right button

        bothPressed = leftPressed && rightPressed;
        controls.enablePan = !bothPressed;
        controls.enableRotate = !bothPressed;
    });

    renderer.domElement.addEventListener("mouseup", (event) => {
        if (event.button === 0) leftPressed = false;
        if (event.button === 2) rightPressed = false;
        
        bothPressed = leftPressed && rightPressed;
        controls.enablePan = !bothPressed;
        controls.enableRotate = !bothPressed;
    });

    renderer.domElement.addEventListener("mousemove", (event) => {
        if (bothPressed) {
            camera.position.y -= event.movementY;
            controls.target.y -= event.movementY;
        }
    });*/
    
    // set screenSpacePanning based on pressed shift key
    window.addEventListener("keydown", (event) => {
        controls.screenSpacePanning = event.shiftKey;
    });

    window.addEventListener("keyup", (event) => {
        controls.screenSpacePanning = event.shiftKey;
    });
}

export function setPosition(camera, x, y, z) {
    camera.position.set(x, y, z);
}

export function lookAt(camera, x, y, z) {
    camera.lookAt(x, y, z);
}

export function updateCamera(camera) {
    if (controls) {
        controls.update();
    }
}