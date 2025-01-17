let renderer, scene, camera, stats;

export function create() {
    camera = new THREE.PerspectiveCamera(90, window.innerWidth / window.innerHeight, 0.1, 1000);
    camera.position.y = 2;
    camera.position.z = 5;
    camera.lookAt(0, 0, 0);

    renderer = new THREE.WebGLRenderer();
    renderer.antialias = true;
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;
    renderer.setSize(window.innerWidth, window.innerHeight);
    renderer.setAnimationLoop(render);

    const canvas = document.querySelector("canvas");
    canvas.parentNode.replaceChild(renderer.domElement, canvas);

    stats = new Stats();
    document.body.appendChild(stats.dom);

    return renderer;
}

export function setScene(newScene) {
    scene = newScene;
}

export function setCamera(newCamera) {
    camera = newCamera;
}

function render() {
    stats.begin();
    renderer.render(scene, camera);
    stats.end();
}