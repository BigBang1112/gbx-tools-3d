import * as THREE from 'three';

const textureLoader = new THREE.TextureLoader();

export function createRandomMaterial() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}

export function createMaterial(diffuseTexture, normalTexture, specularTexture, properties) {
    const material = new THREE.MeshPhongMaterial({
        map: diffuseTexture,
        normalMap: normalTexture,
        specularMap: specularTexture,
        transparent: properties.transparent,
        side: properties.doubleSided ? THREE.DoubleSide : THREE.FrontSide
    });

    if (properties.worldUV) {
        material.onBeforeCompile = (shader) => {
            shader.uniforms.globalUvScale = { value: new THREE.Vector2(1, 1) };

            // Replace the default UV calculation with one based on world position (x, z)
            shader.vertexShader = shader.vertexShader.replace(
                '#include <uv_vertex>',
                [
                    'vec4 worldPosition = modelMatrix * vec4(position, 1.0);',
                    'vUv = worldPosition.xz * globalUvScale;'
                ].join('\n')
            );
        };
    }
}

export function createTexture(path) {
    const texture = textureLoader.load(path);
    texture.wrapS = THREE.RepeatWrapping;
    texture.wrapT = THREE.RepeatWrapping;
    return texture;
}