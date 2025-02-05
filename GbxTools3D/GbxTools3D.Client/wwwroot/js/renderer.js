import * as THREE from 'three';

let renderer, scene, camera, controls, stats;

export function create() {
    THREE.Object3D.DEFAULT_MATRIX_AUTO_UPDATE = false;
    THREE.Object3D.DEFAULT_MATRIX_WORLD_AUTO_UPDATE = false;

    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setAnimationLoop(update);

    const canvas = document.querySelector("canvas");
    canvas.parentNode.replaceChild(renderer.domElement, canvas);

    window.addEventListener('resize', onWindowResize, false);

    stats = new Stats();
    document.body.appendChild(stats.dom);

    return renderer;
}

export function getRenderer() {
    return renderer;
}

export function setScene(newScene) {
    scene = newScene;
}

export function setCamera(newCamera) {
    camera = newCamera;
}

export function setControls(newControls) {
    controls = newControls;
}

function update() {
    stats.begin();
    
    if (controls) {
        controls.update();
    }
    if (scene && camera) {
        renderer.render(scene, camera);
    }
    stats.end();
}

function onWindowResize() {
    // Update camera aspect ratio and renderer size
    camera.aspect = window.innerWidth / window.innerHeight;
    camera.updateProjectionMatrix();
    renderer.setSize(window.innerWidth, window.innerHeight);
}