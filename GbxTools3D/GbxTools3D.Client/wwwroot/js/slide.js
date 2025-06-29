import * as THREE from 'three';

const currentLines = new Map();
const linePointsMap = new Map();
const placedLines = [];
const material = new THREE.MeshStandardMaterial({ color: 0x444444, side: THREE.DoubleSide });
const offset = new THREE.Vector3(0, -0.25, 0);
const lineWidth = 0.3;

// Pomocná funkce pro vytvoření tlusté čáry pomocí trojúhelníků
function buildThickLineGeometry(points) {
    if (points.length < 2) return new THREE.BufferGeometry();
    
    const n = points.length;
    const leftPoints = [];
    const rightPoints = [];
    
    // Vypočítá pro každý bod průměrný směr, pak boční vektor v rovině XZ.
    for (let i = 0; i < n; i++) {
        let dir = new THREE.Vector3();
        if (i === 0) {
            dir.subVectors(points[1], points[0]);
        } else if (i === n - 1) {
            dir.subVectors(points[n - 1], points[n - 2]);
        } else {
            const prev = new THREE.Vector3().subVectors(points[i], points[i - 1]);
            const next = new THREE.Vector3().subVectors(points[i + 1], points[i]);
            dir.addVectors(prev, next);
        }
        // Pokud je směr nulový, nastavíme default směr.
        if (dir.length() === 0) {
            dir.set(1, 0, 0);
        }
        dir.normalize();
        // Vypočítáme boční směr (perpendikulární v rovině XZ)
        const side = new THREE.Vector3(-dir.z, 0, dir.x).normalize().multiplyScalar(lineWidth / 2);
        leftPoints.push(new THREE.Vector3().addVectors(points[i], side));
        rightPoints.push(new THREE.Vector3().subVectors(points[i], side));
    }
    
    // Vytvoříme pole vrcholů: interleaving left a right body
    const vertices = [];
    for (let i = 0; i < n; i++) {
        vertices.push(leftPoints[i].x, leftPoints[i].y, leftPoints[i].z);
        vertices.push(rightPoints[i].x, rightPoints[i].y, rightPoints[i].z);
    }
    
    // Vytvoříme indexy pro dvě trojúhelníky mezi každým párem bodů
    const indices = [];
    for (let i = 0; i < n - 1; i++) {
        const idx = i * 2;
        // Trojúhelník 1: left[i], left[i+1], right[i+1]
        indices.push(idx, idx + 2, idx + 3);
        // Trojúhelník 2: left[i], right[i+1], right[i]
        indices.push(idx, idx + 3, idx + 1);
    }
    
    const geometry = new THREE.BufferGeometry();
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(vertices, 3));
    geometry.setIndex(indices);
    geometry.computeVertexNormals();
    return geometry;
}

export function doSlide(wheelObj, scene) {
    if (!linePointsMap.has(wheelObj)) {
        linePointsMap.set(wheelObj, []);
    }
    const linePoints = linePointsMap.get(wheelObj);

    const wheelWorldPos = new THREE.Vector3();
    wheelObj.getWorldPosition(wheelWorldPos);
    // Přidáme ofset
    linePoints.push(wheelWorldPos.clone().add(offset));

    const geometry = buildThickLineGeometry(linePoints);

    if (currentLines.has(wheelObj)) {
        currentLines.get(wheelObj).geometry.dispose();
        currentLines.get(wheelObj).geometry = geometry;
    }
    else {
        const mesh = new THREE.Mesh(geometry, material);
        mesh.receiveShadow = true;
        currentLines.set(wheelObj, mesh);

        placedLines.push(mesh);
        scene.add(mesh);
    }
}

export function stopSlide(wheelObj) {
    if (!linePointsMap.has(wheelObj)) return;

    const points = linePointsMap.get(wheelObj);
    const wheelWorldPos = new THREE.Vector3();
    wheelObj.getWorldPosition(wheelWorldPos);
    points.push(wheelWorldPos.clone().add(offset));

    const geometry = buildThickLineGeometry(points);
    if (currentLines.has(wheelObj)) {
        let mesh = currentLines.get(wheelObj);
        mesh.userData.ended = true;
        mesh.geometry.dispose();
        mesh.geometry = geometry;
    }

    // Vyčištění
    currentLines.delete(wheelObj);
    linePointsMap.delete(wheelObj);
}