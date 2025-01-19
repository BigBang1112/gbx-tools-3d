import * as THREE from 'three';

export function create() {
    return new THREE.Scene();
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

    // Create an ambient light
    var ambientLight = new THREE.AmbientLight(0x7F7F7F);

    // Add the light to the scene
    add(scene, ambientLight);

    // Create a directional light
    let directionalLight = new THREE.DirectionalLight(0xffffff, 1);

    // Set the position and direction of the light
    directionalLight.position.set(0, 256, 0);
    directionalLight.target.position.set(-2048, 0, -2048);

    // Set up shadow properties for the light
    directionalLight.castShadow = true;

    directionalLight.shadow.camera.up.set(0, 0, 1);
    directionalLight.shadow.camera.lookAt(0, 0, 0);

    // Add the light to the scene
    add(scene, directionalLight);
}

export function add(scene, obj) {
    scene.add(obj);
}