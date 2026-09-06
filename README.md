# burning-tree-campfire-3d

> Generated autonomously by **OmniUnity Chat AI Agent**

## Multi-Platform Cloud CI/CD (GitHub Actions)

This project is configured with 3 build strategies in `.github/workflows/`:

1. **Cloud Matrix (`unity-build-matrix.yml`)**:
   - Compiles across **Windows x64**, **Linux x64**, **WebGL (HTML5)**, and **Android (APK)** in parallel on GitHub `ubuntu-latest`.
   - Requires repository secrets `UNITY_LICENSE` (or `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`).

2. **Acquire Activation File (`activation.yml`)**:
   - 1-click manual workflow dispatch to get your Unity `.alf` activation file.
   - Upload the resulting `.alf` file to [license.unity3d.com](https://license.unity3d.com/manual) to download your free `.ulf` license, then save it as `UNITY_LICENSE` in GitHub Secrets.

3. **Self-Hosted Runner (`unity-build-self-hosted.yml`)**:
   - Zero-secrets native build if you attach a self-hosted runner. Compiles instantly using your machine's pre-activated Unity Editor.
