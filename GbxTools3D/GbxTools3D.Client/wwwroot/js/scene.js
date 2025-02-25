import * as THREE from 'three';

export function create() {
    const scene = new THREE.Scene();
    add(scene, new THREE.AmbientLight(0x7F7F7F));
    add(scene, createDirectionalLight());
    scene.updateMatrixWorld();
    return scene;
}

export function test(scene) {
    const geometry = new THREE.BoxGeometry(1, 1, 1);
    const material = new THREE.MeshStandardMaterial({ color: 0xffff00 });
    const cube = new THREE.Mesh(geometry, material);
    cube.receiveShadow = true;
    cube.castShadow = true;
    add(scene, cube);

    // add plane
    const planeGeometry = new THREE.PlaneGeometry(100, 100, 1, 1);
    const planeMaterial = new THREE.MeshStandardMaterial({ color: 0x006600 });
    const plane = new THREE.Mesh(planeGeometry, planeMaterial);
    plane.rotation.x = -Math.PI / 2;
    plane.position.y = -1;
    plane.receiveShadow = true;
    add(scene, plane);

    // Create a directional light
    let directionalLight = new THREE.DirectionalLight(0xffffff, 1);

    // Set the position and direction of the light
    directionalLight.position.set(0, 16, 0);
    directionalLight.target.position.set(32, 0, 32);

    // Set up shadow properties for the light
    directionalLight.castShadow = true;

    // Add the light to the scene
    add(scene, directionalLight);

    const helper = new THREE.CameraHelper( directionalLight.shadow.camera );
    add(scene, helper);

    const gridHelper = new THREE.GridHelper(1024, 1024);
    add(scene, gridHelper);
}

export function add(scene, obj) {
    scene.add(obj);
}

export function remove(scene, obj) {
    scene.remove(obj);
}

function createDirectionalLight() {
    const light = new THREE.DirectionalLight(0xffffff, 1);
    light.matrixAutoUpdate = true;
    light.matrixWorldAutoUpdate = true;
    light.shadow.matrixAutoUpdate = true;
    light.shadow.matrixWorldAutoUpdate = true;
    light.shadow.camera.matrixAutoUpdate = true;
    light.shadow.camera.matrixWorldAutoUpdate = true;

    light.position.set(3072, 2048, 3072); // Position the light in the scene
    light.target.position.set(0, 0, 0); // Point the light at the center

    //light.shadow.camera.position.set(1024*8, 1024, 1024*8); // Position the light in the scene
    //light.shadow.camera.lookAt(0, 0, 0); // Point the light at the center

    // Enable shadows
    light.castShadow = true;

    // Adjust shadow map size for better quality
    light.shadow.mapSize.width = 4096;
    light.shadow.mapSize.height = 4096;

    // Set shadow camera parameters to cover the 1024x1024 scene
    light.shadow.camera.left = -2048;
    light.shadow.camera.right = 2048;
    light.shadow.camera.top = 2048;
    light.shadow.camera.bottom = -2048;
    light.shadow.camera.near = 512;
    light.shadow.camera.far = 5120;
    //light.shadow.autoUpdate = false;
    //light.shadow.needsUpdate = true;

    return light;
}