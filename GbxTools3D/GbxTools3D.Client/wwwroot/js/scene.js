export function create() {
    return new THREE.Scene();
}

export function test(scene) {
    const geometry = new THREE.BoxGeometry(1, 1, 1);
    const material = new THREE.MeshBasicMaterial({ color: 0x00ff00 });
    const cube = new THREE.Mesh(geometry, material);
    cube.castShadow = true;
    add(scene, cube);
}

export function add(scene, obj) {
    scene.add(obj);
}