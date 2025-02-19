using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using MixedReality.Toolkit.SpatialManipulation;
using DG.Tweening;
using UnityEngine.SceneManagement;
using MixedReality.Toolkit;
public class StepManager : MonoBehaviour
{
    [Header("Prefabs e Objetos")]
    public GameObject mainPCPrefab; // Referência ao prefab da cena
    public GameObject newRamPrefab; // Prefab da nova RAM
    public Vector3 mainPCPrefabSpawnPosition = new Vector3(2.5f, -7f, 2.6f); // Posição inicial do objeto na cena
    public GameObject chevronPrefab; // Prefab do chevron

    [Header("Materiais")]
    public Material highlightMaterial; // Material de destaque (vermelho)
    public Material originalMaterial; // Para armazenar o material original do objeto
    public Material nearlyInvisibleMaterial; // Material quase invisível

    [Header("UI")]
    public TextMeshProUGUI SlateText; // Texto do slate

    private GameObject mainPCPrefabeInstance; // Instância do prefab na cena
    private Transform screws, cover, ram; // Componentes individuais
    private GameObject tampoClone, screwsClone, ramClone, newRamClone; // Clones dos componentes
    private Vector3 newRAMPosition;

    private int currentStep = 0; // Passo atual

    // Reference to the DirectionalIndicator component
    private DirectionalIndicator directionalIndicator;

    // New target for the chevron to point toward
    private GameObject animatedObject;

    void Start()
    {
        LoadPrefab();

        Component[] components = chevronPrefab.GetComponents<Component>();
        foreach (Component component in components)
        {
            Debug.Log(component.GetType().Name);
        }

        // Get the DirectionalIndicator component from the chevronPrefab
        directionalIndicator = chevronPrefab.GetComponent<DirectionalIndicator>();
        if (directionalIndicator != null)
        {
            Debug.Log("DirectionalIndicator component found!");
        }
        else
        {
            Debug.LogError("DirectionalIndicator component not found!");
        }


        InitializeComponents();
        StartStep();
    }
    
    // Método para carregar o prefab na cena
    void LoadPrefab()
    {
        if (mainPCPrefab != null)
        {
            // Instancia o prefab na posição especificada
            mainPCPrefabeInstance = Instantiate(mainPCPrefab, mainPCPrefabSpawnPosition, Quaternion.identity);
            Debug.Log("PC Prefab carregado com sucesso!");
        }
        else
        {
            Debug.LogError("Prefab não atribuído ao ScriptStepLoader!");
        }
    }

    // Inicializa as referências aos componentes
    void InitializeComponents()
    {
        if (mainPCPrefabeInstance != null)
        {
            screws = mainPCPrefabeInstance.transform.Find("parafusos");
            cover = mainPCPrefabeInstance.transform.Find("tampo");
            ram = mainPCPrefabeInstance.transform.Find("ram_LP");

            if (screws == null || cover == null || ram == null)
            {
                Debug.LogError("Um ou mais componentes não foram encontrados no prefab!");
            }
        }
        else
        {
            Debug.LogError("PC Instance não atribuída!");
        }
    }

    // Inicia o passo atual
    void StartStep()
    {
        switch (currentStep)
        {
            case 0:
                SlateText.text = "Passo 1: \n Localize os parafusos indicados em vermelho na parte de trás da torre. \n Esses parafusos seguram a tampa lateral da torre. \n Agarre e afaste para longe para simular a remoção.";
                directionalIndicator.DirectionalTarget = screws;
                HighlightComponent(screws, "Destacando os parafusos para remoção.");
                break;

            case 1:
                SlateText.text = "Passo 2: \n Agora, remova a tampa lateral indicada em vermelho. \n Agarre a tampa e afaste para fora. \n Isso permitirá acesso ao interior da torre.";
                directionalIndicator.DirectionalTarget = cover;
                HighlightComponent(cover, "Destacando a tampa para remoção.");
                break;

            case 2:
                SlateText.text = "Passo 3: \n A memória RAM que precisa ser substituída está indica a vermelho dentro da torre. \n Agarre a RAM indicada e afaste para simular a remoção";
                directionalIndicator.DirectionalTarget = ram;
                HighlightComponent(ram, "Destacando a RAM para substituição.");
                break;

            case 3:
                SlateText.text = "Passo 4: \n Adicione a nova RAM ao slot vazio. \n Agarre a RAM, alinhe com o slot e ponha de volta no local do passo anterior.";
                AddComponent(newRamPrefab, ram.position, ram.rotation, "Adicionando a nova RAM.");
                break;
            
            case 4:
                SlateText.text = "Passo 5: \n Recoloque a tampa lateral. \n Agarre a tampa removida nos passos anteriores e leve-a de volta à posição original.";
                PutBackComponent(cover.gameObject, "Por de volta a tampa");
                break;

            case 5:
                SlateText.text = "Passo 6: \n Recoloque os parafusos na parte de trás da torre. \n Agarre os parafuso removidos nos passos anteriores e leve-a de volta à posição original.";
                PutBackComponent(screws.gameObject, "Por de volta o parafuso");
                break;

            default:
                SlateText.text = "Todos os passos foram concluídos!";
                Debug.Log("Todos os passos foram concluídos!");
                break;
        }
    }

    void HighlightComponent(Transform component, string message)
    {
        Debug.Log(message);
        if (component != null)
        {
            Renderer renderer = component.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Cria uma cópia do componente e aplica o material de destaque
                GameObject highlightedComponent = Instantiate(component.gameObject, component.position, component.rotation);
                if(currentStep == 0){
                    screwsClone = highlightedComponent;
                }
                if(currentStep == 1){
                    tampoClone = highlightedComponent;
                }
                if(currentStep == 2){
                    ramClone = highlightedComponent;
                }

                Renderer highlightedRenderer = highlightedComponent.GetComponent<Renderer>();
                highlightedRenderer.material = highlightMaterial;

                // Torna o componente original invisível
                renderer.enabled = false;

                if(currentStep == 2){
                    // Desativa o collider do mainPCPrefabeInstance para não interferir na movimentação da RAM
                    Collider mainPCCollider = mainPCPrefabeInstance.GetComponent<Collider>();

                    if (mainPCCollider != null)
                    {
                        mainPCCollider.enabled = false;
                    }
                }

                // Inicia a verificação de movimento
                StartCoroutine(CheckMovement(component, highlightedComponent));
            }
            else
            {
                Debug.LogError("Renderer não encontrado no componente!");
            }
        }
        else
        {
            Debug.LogError("Componente não encontrado!");
        }
    }

    IEnumerator CheckMovement(Transform originalComponent, GameObject highlightedComponent)
    {
        Vector3 initialPCPosition = mainPCPrefabeInstance.transform.position;
        Quaternion initialPCRotation = mainPCPrefabeInstance.transform.rotation;

        // Store the local position relative to the main PC prefab
        Vector3 localComponentPosition = mainPCPrefabeInstance.transform.InverseTransformPoint(highlightedComponent.transform.position);

        while (true)
        {
            // Check if the main PC prefab moved or rotated
            if (Vector3.Distance(mainPCPrefabeInstance.transform.position, initialPCPosition) > 0.01f ||
                Quaternion.Angle(mainPCPrefabeInstance.transform.rotation, initialPCRotation) > 0.5f)
            {
                // Update the highlighted component's world position based on its stored local position
                highlightedComponent.transform.position = mainPCPrefabeInstance.transform.TransformPoint(localComponentPosition);

                // Apply the new rotation
                Quaternion rotationDifference = mainPCPrefabeInstance.transform.rotation * Quaternion.Inverse(initialPCRotation);
                highlightedComponent.transform.rotation = rotationDifference * highlightedComponent.transform.rotation;

                // Update the reference positions
                initialPCPosition = mainPCPrefabeInstance.transform.position;
                initialPCRotation = mainPCPrefabeInstance.transform.rotation;

                Debug.LogWarning("PC moved or rotated, adjusting component position.");
            }

            // Check if the component has moved too far from its initial position
            if (Vector3.Distance(highlightedComponent.transform.position, mainPCPrefabeInstance.transform.TransformPoint(localComponentPosition)) > 0.5f)
            {
                SetACTIVEComponent(originalComponent, highlightedComponent, "Componente movido, passo concluído.");
                yield break;
            }

            yield return null;
        }
    }

    void SetACTIVEComponent(Transform component, GameObject highlightedComponent, string message)
    {
        Debug.Log(message);
        if (component != null)
        {
            Renderer renderer = component.GetComponent<Renderer>();
            if (renderer != null)
            {
                Renderer highlightedRenderer = highlightedComponent.GetComponent<Renderer>();
                highlightedRenderer.material = originalMaterial;
            }

            // Ativa o componente
            if(currentStep == 2){
                // Desativa o collider do mainPCPrefabeInstance para não interferir na movimentação da RAM
                Collider mainPCCollider = mainPCPrefabeInstance.GetComponent<Collider>();
                
                if (mainPCCollider != null)
                {
                    mainPCCollider.enabled = true;
                }
            }

            // Desativa o componente
            component.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("Componente não encontrado!");
        }

        // Avança para o próximo passo
        currentStep++;
        StartStep();
    }

    void AddComponent(GameObject newComponentPrefab, Vector3 position, Quaternion rotation, string message)
    {
        Debug.Log(message);
        if (newComponentPrefab != null)
        {
            if(currentStep == 3){
                // Desativa o collider do mainPCPrefabeInstance para não interferir na movimentação da RAM
                Collider mainPCCollider = mainPCPrefabeInstance.GetComponent<Collider>();
                
                if (mainPCCollider != null)
                {
                    mainPCCollider.enabled = false;
                }
            }

            // Ajusta a posição para colocar o novo componente ao lado do mainPCPrefabeInstance
           newRAMPosition = mainPCPrefabeInstance.transform.position + new Vector3(0.4f, 8.5f, 0.9f); // Ajuste conforme necessário

            // Instancia o novo componente na posição e rotação especificadas
            newRamClone = Instantiate(newComponentPrefab, newRAMPosition, rotation);
            newRamClone.transform.SetParent(mainPCPrefabeInstance.transform, true);

            directionalIndicator.DirectionalTarget = newRamClone.transform;

            if(currentStep == 3){
                // Ativa a RAM original com um material quase invisível
                Renderer ramRenderer = ram.GetComponent<Renderer>();
                if (ramRenderer != null)
                {
                    ramRenderer.enabled = true;
                    ramRenderer.material = nearlyInvisibleMaterial;
                }

                ram.gameObject.SetActive(true);
            }

            // Inicia a verificação de posicionamento
            StartCoroutine(CheckNewComponentPosition(newRamClone, ram.position));
        }
        else
        {
            Debug.LogError("Prefab do novo componente não atribuído!");
        }
    }

    IEnumerator CheckNewComponentPosition(GameObject newRamClone, Vector3 targetPosition)
    {
        while (true)
        {
            // Verifica se o novo componente está próximo da posição alvo
            if (currentStep == 3 && (Vector3.Distance(newRamClone.transform.position, targetPosition) < 0.05f))
            {
                Debug.Log("Novo componente posicionado corretamente.");
                newRamClone.SetActive(false);
                Renderer ramRenderer = ram.GetComponent<Renderer>();
                if (ramRenderer != null)
                {
                    ramRenderer.material = originalMaterial;
                }
                //ram.gameObject.SetActive(false);
                if(currentStep == 3){
                    // Desativa o collider do mainPCPrefabeInstance para não interferir na movimentação da RAM
                    Collider mainPCCollider = mainPCPrefabeInstance.GetComponent<Collider>();
                    
                    if (mainPCCollider != null)
                    {
                        mainPCCollider.enabled = true;
                    }
                }

                // Avança para o próximo passo
                currentStep++;
                StartStep();

                yield break; // Encerra a coroutine
            } else {
                ram.gameObject.SetActive(true);
            }
            yield return null;
        }
    }

    public void PutBackComponent(GameObject component, string message)
    {
        Debug.Log(message);
        // Change the cover's material to red
        Renderer highlightedRenderer = component.GetComponent<Renderer>();

        // Activate the renderer
        highlightedRenderer.enabled = true;

        // Set the material to nearlyInvisibleMaterial
        highlightedRenderer.material = nearlyInvisibleMaterial;

        if(currentStep == 4){
            Renderer tampoRender = tampoClone.GetComponent<Renderer>();
            tampoRender.material = highlightMaterial;
            directionalIndicator.DirectionalTarget = tampoClone.transform;
        }

        if(currentStep == 5){
            Renderer screwsRender = screwsClone.GetComponent<Renderer>();
            screwsRender.material = highlightMaterial;
            directionalIndicator.DirectionalTarget = screwsClone.transform;
        }
        
        // Activate the object
        component.SetActive(true);

        if(currentStep == 4){
            // Assuming tampoClone is a GameObject
            Transform tampoCloneTransform = tampoClone.transform;
            StartCoroutine(CheckMatch(component.transform, tampoCloneTransform));
        }

        if(currentStep == 5){
            // Assuming screwsClone is a GameObject
            Transform screwsCloneTransform = screwsClone.transform;
            StartCoroutine(CheckMatch(component.transform, screwsCloneTransform));
        }
    }

    private IEnumerator CheckMatch(Transform component, Transform originalClone)
    {
        while (true)
        {
            // Check if the positions match
            if (Vector3.Distance(component.position, originalClone.position) < 0.05f)
            {
                Debug.Log("Componente colocado de volta corretamente.");
            
                // Set the originalClone's active state to false
                originalClone.gameObject.SetActive(false);
                Renderer highlightedRenderer = component.GetComponent<Renderer>();
                highlightedRenderer.material = originalMaterial;

                // Advance to the next step
                currentStep++;
                StartStep();

                yield break;
            }

            // Wait for the next frame before checking again
            yield return null;
        }
    }

    public void RestartSteps(){
        // Reload the current scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
    private bool animationInProgress = false;
    public void OnActionButtonClick()
    {
        if (animationInProgress)
        {
            Debug.Log("Animation is still in progress.");
            return;
        }

        animationInProgress = true;

        Vector3 animationScrew = screws.position - new Vector3(0.3f, 0, 0); 
        Vector3 animationCover = cover.position - new Vector3(0.3f, 0, 0);
        Vector3 animationRam = ram.position - new Vector3(0.3f, 0, 0);
        switch (currentStep)
        {
            case 0:
                animatedObject = Instantiate(screwsClone, screws.position, screws.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(animationScrew, 5.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;

            case 1:
                animatedObject = Instantiate(tampoClone, cover.position, cover.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(animationCover, 5.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;

            case 2:
                animatedObject = Instantiate(ramClone, ram.position, ram.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(animationRam, 5.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;

            case 3:
                animatedObject = Instantiate(newRamClone, newRamClone.transform.position, newRamClone.transform.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(ram.position, 6.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;

            case 4:
                animatedObject = Instantiate(tampoClone, tampoClone.transform.position, tampoClone.transform.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(cover.position, 6.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;

            case 5:
                animatedObject = Instantiate(screwsClone, screwsClone.transform.position, screwsClone.transform.rotation);
                animatedObject.GetComponent<Renderer>().material = nearlyInvisibleMaterial;
                animatedObject.transform.DOLocalMove(screws.position, 6.0f)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Destroy(animatedObject);
                        animationInProgress = false;
                    });
                break;
        }
    }
}
