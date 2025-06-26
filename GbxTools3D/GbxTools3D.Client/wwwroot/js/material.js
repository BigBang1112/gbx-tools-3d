import * as THREE from 'three';
import waterVertexShader from '../shaders/water.vert?raw';
import waterFragmentShader from '../shaders/water.frag?raw';

const textureLoader = new THREE.TextureLoader();

const wireframeCollisionMaterial = new THREE.MeshBasicMaterial({ wireframe: true, color: 0x00ff00 });
const wireframeMaterial = new THREE.MeshBasicMaterial({ wireframe: true, color: 0xDDDDDD });

export function getWireframeMaterial() {
    return wireframeMaterial;
}

export function createRandomMaterial() {
    return new THREE.MeshStandardMaterial({ color: Math.floor(Math.random()*16777215) });
}

export function createInvisibleMaterial() {
    return new THREE.MeshBasicMaterial({ visible: false });
}

export function createGlassMaterial() {
    return new THREE.MeshPhysicalMaterial({
        color: 0xFFFFFF,
        metalness: 0.1,
        roughness: 0,
        transmission: 0.8,
        thickness: 0.1,
        transparent: true,
        alphaToCoverage: true,
        depthWrite: false
    });
}

export function createMaterial(
    materialName,
    shaderName,
    diffuseTexture,
    normalTexture,
    specularTexture,
    blend2Texture,
    blendIntensityTexture,
    blend3Texture,
    aoTexture,
    doubleSided,
    worldUV,
    transparent,
    basic,
    specularAlpha,
    opacity,
    add,
    nightOnly,
    invisible,
    water,
    substract) {
    let material;
    if (basic) {
        material = new THREE.MeshBasicMaterial({
            map: diffuseTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide
        });
    }
    else if (water) {
        material = new THREE.ShaderMaterial({
            vertexShader: waterVertexShader,
            fragmentShader: waterFragmentShader,
            uniforms: {
                uTime: { value: 0 },
                uOpacity: { value: 0.8 },
                /*uEnvironmentMap: { value: options.environmentMap },*/
                uWavesAmplitude: { value: 0.025 },
                uWavesFrequency: { value: 1.07 },
                uWavesPersistence: { value: 0.3 },
                uWavesLacunarity: { value: 2.18 },
                uWavesIterations: { value: 8 },
                uWavesSpeed: { value: 0.4 },
                uTroughColor: { value: new THREE.Color('#186691') },
                uSurfaceColor: { value: new THREE.Color('#9bd8c0') },
                uPeakColor: { value: new THREE.Color('#bbd8e0') },
                uPeakThreshold: { value: 0.08 },
                uPeakTransition: { value: 0.05 },
                uTroughThreshold: { value: -0.01 },
                uTroughTransition: { value: 0.15 },
                uFresnelScale: { value: 0.8 },
                uFresnelPower: { value: 0.5 }
            },
            transparent: true,
            depthTest: true,
            side: THREE.DoubleSide
        });
        /*material = new THREE.MeshPhysicalMaterial({
            transmission: 0.7,
            roughness: 0.1,
            thickness: 0.1,
            color: 0xddeeff
        });*/
    }
    else if (specularAlpha || specularTexture)
    {
        material = new THREE.MeshPhongMaterial({
            map: diffuseTexture,
            normalMap: normalTexture,
            specularMap: specularTexture,
            aoMap: aoTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide,
        });
    }
    else {
        material = new THREE.MeshStandardMaterial({
            map: diffuseTexture,
            normalMap: normalTexture,
            aoMap: aoTexture,
            transparent: transparent,
            alphaToCoverage: transparent,
            side: doubleSided ? THREE.DoubleSide : THREE.FrontSide,
            blending: add ? THREE.AdditiveBlending : (substract ? THREE.SubtractiveBlending : THREE.NormalBlending),
            depthWrite: !add, // some stadium transparency sorting problems
            visible: !invisible,
        });
    }

    let key = 0;
    if (specularAlpha) key |= 1;
    if (opacity !== 0) key |= 2;
    if (blend2Texture && blendIntensityTexture) key |= 4;
    else if (blend3Texture) key |= 8;
    if (worldUV && material.map) key |= 16;

    material.onBeforeCompile = (shader) => {
        if (specularAlpha) {
            // set specular based on diffuse alpha, not sure if this does something yet
            shader.fragmentShader.replace(
                '#include <specularmap_fragment>',
                `
                #include <specularmap_fragment>
                specularStrength = sampledDiffuseColor.a;
            `);
        }

        if (opacity !== 0) {
            // set specular based on diffuse alpha, not sure if this does something yet
            shader.uniforms.customOpacity = { value: opacity };

            shader.fragmentShader = `
                uniform float customOpacity;

                ${shader.fragmentShader}`.replace(
                '#include <map_fragment>',
                `
                    #include <map_fragment>
                    diffuseColor.a = customOpacity;
                `
            );
        }

        if (blend2Texture && blendIntensityTexture) {
            shader.uniforms.blend2Texture = { value: blend2Texture };
            shader.uniforms.blendIntensityTexture = { value: blendIntensityTexture };
            shader.uniforms.blend3Texture = { value: blend3Texture };
            shader.fragmentShader = `
                uniform sampler2D blend2Texture;
                uniform sampler2D blendIntensityTexture;
                uniform sampler2D blend3Texture;
                ${shader.fragmentShader}`.replace('#include <map_fragment>', `
                #include <map_fragment>
                vec4 blend2Color = texture2D(blend2Texture, vMapUv);
                float blendIntensity = texture2D(blendIntensityTexture, vMapUv).r;
                vec4 blend3Color = texture2D(blend3Texture, vMapUv);
                diffuseColor = mix(mix(diffuseColor, blend2Color, blendIntensity), blend3Color, blend3Color.a);
                `);
        }
        else if (blend3Texture) {
            shader.uniforms.blend3Texture = { value: blend3Texture };
            shader.fragmentShader = `
                uniform sampler2D blend3Texture;
                ${shader.fragmentShader}`.replace('#include <map_fragment>', `
                #include <map_fragment>
                vec4 blend3Color = texture2D(blend3Texture, vMapUv);
                diffuseColor = mix(diffuseColor, blend3Color, blend3Color.a);
                `);
        }

        if (worldUV && material.map) {
            shader.uniforms.uvTransform = { value: new THREE.Matrix3().set(1 / 16, 0, 0, 0, 1 / 16, 0, 0, 0, 1) };

            shader.vertexShader = `
                uniform mat3 uvTransform;
          
                ${shader.vertexShader}`.replace('#include <uv_vertex>\n', '')
                .replace(
                    '#include <worldpos_vertex>',
                    `vec4 worldPosition = vec4(transformed, 1.0);
                    #ifdef USE_INSTANCING
                    worldPosition = instanceMatrix * worldPosition;
                    #endif
                    worldPosition = modelMatrix * worldPosition;
                    vMapUv = (uvTransform * vec3(worldPosition.xz, 1)).xy;`
            );
        }
    };

    material.customProgramCacheKey = function () {
        return key.toString();
    }

    material.name = materialName;
    if (shaderName) {
        material.userData.shaderName = shaderName;
    }

    return material;
}

export function createTexture(path, urlPath, noWrap) {
    const texture = textureLoader.load(urlPath);
    if (!noWrap) {
        texture.wrapS = THREE.RepeatWrapping;
        texture.wrapT = THREE.RepeatWrapping;
    }
    texture.name = path;
    return texture;
}