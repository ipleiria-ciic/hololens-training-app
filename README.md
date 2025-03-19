# SafeAR - Support Privacy and Confidentiality in Augmented Reality Contexts

HoloLens application for training using real-time privacy protection.

SafeAR is an exploratory research project funded by FCT (Fundação para a Ciência e a Tecnologia).

## Setup and Deployment Guide

### Prerequisites

Before proceeding, ensure that you have the following installed:

1. **Unity** (At Least 2022.3.15f1 version recommended)
2. **Visual Studio** (With UWP and HoloLens development support)
3. **Windows** 10/11 SDK
4. **HoloLens 2 Device** or **HoloLens Emulator**

## Checking the Project Configuration

Before building and deploying, confirm that the project is set to UWP (Universal Windows Platform):

1. Open Unity and load the project.
2. Navigate to `File > Build Settings`.
3. Ensure the platform is set to **Universal Windows Platform (UWP)**.
4. Navigate to `Edit > Project Settings... > XR Plug-in Management`.
5. Ensure **Open XR** is selected and **Microsoft HoloLens Feature Group**.

## Building the Project

1. In Unity, go to `File > Build Settings`.
2. Select **UWP** and configure as described above.
3. Click **Build** and select a new folder (e.g., Build/).
4. Wait for the build process to complete.

## Opening in Visual Studio and Deploying

1. Open **Visual Studio**.
2. Load the `.sln` file located in the build folder.
3. Set the build configuration to:
    * Release
    * ARM64
4. Connect your HoloLens device via USB or ensure it is available over Wi-Fi.
5. Select **Device** or **Remote Machine** as the deployment target.
6. Click **Deploy** to install and run the application on HoloLens.

## Scenes Overview

### 1. Image Obfuscation Scene

![image](https://github.com/user-attachments/assets/59f8627e-ab3e-4af0-8a17-b82f645c0f6a)

* This scene processes images to obfuscate sensitive information.

* **Important**: For the obfuscation feature to work, you must be connected to **IPLeiria wi-fi** or by the **IPLeiria VPN**. If is not possible to connect to this VPN or wi-fi, you can use [safeAR](https://github.com/ipleiria-ciic/safe-ar/) server.

### 2. Tutorial Scene

![image](https://github.com/user-attachments/assets/6bc9ea47-22ee-4f18-9f04-b10612ec1794)

* Provides a game with step-by-step tutorial on how to change a **RAM** from a **computer**.

## Acknowledgements
This work is funded by FCT - Fundação para a Ciência e a Tecnologia, I.P., through project with reference 2022.09235.PTDC.

![Acknowledgements](https://github.com/user-attachments/assets/0bcd9f5f-ad74-4351-a8ac-78e0a10e0a89)
