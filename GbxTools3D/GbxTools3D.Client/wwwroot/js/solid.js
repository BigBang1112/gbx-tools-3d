import * as THREE from 'three';

export function create() {
    return new THREE.Object3D();
}

export function add(parent, child) {
    parent.add(child);
}

export function setPosition(tree, x, y, z) {
    tree.position.set(x, y, z);
}

export function setRotation(tree, xx, xy, xz, yx, yy, yz, zx, zy, zz) {
    tree.rotation.setFromRotationMatrix(new THREE.Matrix4().set(xx, xy, xz, 0, yx, yy, yz, 0, zx, zy, zz, 0, 0, 0, 0, 1));
}

export function createLod() {
    return new THREE.LOD();
}

export function addLod(lodTree, levelTree, distance) {
    lodTree.addLevel(levelTree, distance);
}

export function createVisual(vertData, normData, indData, uvData, expectedMeshCount) {
    var verts = new Float32Array(vertData.length / 4);
    var vertDataView = new DataView(vertData.slice().buffer);
    for (var i = 0; i < vertData.length; i += 4) {
        verts[i / 4] = vertDataView.getFloat32(i, true);
    }

    var norms = new Float32Array(normData.length / 4);
    var normDataView = new DataView(normData.slice().buffer);
    for (var i = 0; i < normData.length; i += 4) {
        norms[i / 4] = normDataView.getFloat32(i, true);
    }

    var inds = new Int32Array(indData.length);
    indData.copyTo(inds);

    const geometry = new THREE.BufferGeometry();
    geometry.setIndex(new THREE.Uint32BufferAttribute(inds, 1));
    geometry.setAttribute('position', new THREE.Float32BufferAttribute(verts, 3));
    geometry.setAttribute('normal', new THREE.Float32BufferAttribute(norms, 3));

    var vertCount = verts.length / 3;
    var uvSetCount = uvData.length / 8 / vertCount;
    var uvBufferCount = uvData.length / uvSetCount;

    for (var i = 0; i < uvSetCount; i++) {
        var offset = i * uvBufferCount;
        var uvDataView = new DataView(uvData.slice(offset, offset + uvBufferCount).buffer);
        var uvs = new Float32Array(uvBufferCount / 4);
        for (var j = 0; j < uvBufferCount; j += 4) {
            uvs[j / 4] = uvDataView.getFloat32(j, true);
        }

        if (i == 0) {
            geometry.setAttribute('uv', new THREE.Float32BufferAttribute(uvs, 2));
        }
    }

    if (uvSetCount > 0) {
        geometry.computeTangents();
    }

    const mesh = new THREE.InstancedMesh(geometry, new THREE.MeshMatcapMaterial({ color: 0xAD9000 }), expectedMeshCount);
    mesh.receiveShadow = true;
    mesh.castShadow = true;

    return mesh;
}