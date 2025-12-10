# PlayMode Tests Setup Guide

## ?? Panoramica

I **PlayMode tests** servono per testare:
- ? Comportamento della Matta con UI (sprite swap, marker, hover)
- ? Interazioni utente (click, drag & drop)
- ? Integrazione tra TurnController, CardViewManager, e CardView
- ? Coroutine e timing (AI delays, animazioni)
- ? Flusso completo del gioco

## ?? Configurazione Assembly Definition

### File: `Assets/Tests/PlayMode/Project51.PlayModeTests.asmdef`

```json
{
    "name": "Project51.PlayModeTests",
    "rootNamespace": "Project51.PlayModeTests",
    "references": [
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner",
        "Project51.Core",
        "Project51.Gameplay"
    ],
    "includePlatforms": [],
    "excludePlatforms": [],
    "allowUnsafeCode": false,
    "overrideReferences": true,
    "precompiledReferences": [
        "nunit.framework.dll"
    ],
    "autoReferenced": false,
    "defineConstraints": [
        "UNITY_INCLUDE_TESTS"
    ],
    "versionDefines": [],
    "noEngineReferences": false
}
```

### ?? IMPORTANTE: Refresh Unity dopo aver modificato .asmdef

1. Salva tutte le modifiche ai file `.asmdef`
2. **Torna in Unity Editor**
3. **Menu: Assets ? Reimport All** (o aspetta il refresh automatico)
4. **Menu: Assets ? Refresh** (Ctrl+R)
5. Aspetta che Unity ricompili tutti gli assembly

## ?? Struttura Directory

```
Assets/Tests/
??? Editor/                          # Test logica pura (veloci)
?   ??? AccusiAndPunteggioComprehensiveTests.cs
?   ??? CirullaAITests.cs
?   ??? Rules51CoreTests.cs
?
??? PlayMode/                        # Test integrazione (realistici)
    ??? Project51.PlayModeTests.asmdef
    ??? README_PlayModeTests.md      # Questa guida
    ??? MattaBehaviorPlayModeTests.cs    # Test Matta UI
    ??? CardInteractionPlayModeTests.cs  # Test click/drag
    ??? TurnFlowPlayModeTests.cs         # Test flusso completo
```

## ?? Template per PlayMode Test

### Esempio: Test Matta Behavior

```csharp
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Project51.Core;
using Project51.Unity;

namespace Project51.PlayModeTests
{
    public class MattaBehaviorPlayModeTests
    {
        private GameObject testSceneRoot;
        private TurnController turnController;

        [UnitySetUp]
        public IEnumerator Setup()
        {
            // Crea scena di test
            testSceneRoot = new GameObject("TestScene");
            turnController = testSceneRoot.AddComponent<TurnController>();
            
            // Aspetta che Start() sia eseguito
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator Teardown()
        {
            if (testSceneRoot != null)
            {
                Object.Destroy(testSceneRoot);
            }
            yield return null;
        }

        [UnityTest]
        public IEnumerator Matta_WithDecino_ShowsCorrectValue()
        {
            // Arrange
            var hand = new List<Card>
            {
                new Card(Suit.Coppe, 7),  // Matta
                new Card(Suit.Denari, 6),
                new Card(Suit.Bastoni, 6)  // Coppia -> Decino
            };
            
            turnController.SetupScenarioForCurrentPlayer(hand, new List<Card>());
            
            // Act - aspetta che UI si aggiorni
            yield return new WaitForSeconds(0.3f);
            
            // Assert
            var mattaView = Object.FindObjectsOfType<CardView>()
                .FirstOrDefault(cv => cv.Card.IsMatta);
            
            Assert.NotNull(mattaView, "Matta CardView dovrebbe esistere");
        }
    }
}
```

## ?? Come Eseguire i Test

### Da Unity Editor:

1. **Menu: Window ? General ? Test Runner**
2. **Tab: PlayMode**
3. Click su **Run All** o seleziona test specifici
4. I test vengono eseguiti **in play mode** (più lenti ma realistici)

### Da Command Line:

```bash
# Windows
Unity.exe -runTests -testPlatform playmode -projectPath "C:\Unity\51"

# macOS/Linux
Unity -runTests -testPlatform playmode -projectPath "/path/to/project"
```

## ?? Cosa Testare nei PlayMode Tests

### 1. Matta Visual Behavior
- ? Sprite cambia quando c'è Decino/Cirulla
- ? Marker appare sopra la carta
- ? Hover mostra 7 di Coppe originale
- ? Exit hover ripristina valore temporaneo

### 2. Card Interactions
- ? Click su carta seleziona/deseleziona
- ? Double-click su carta senza catture esegue PlayOnly
- ? Drag & drop su carte tavolo esegue cattura
- ? Invalid drag mostra feedback errore

### 3. Turn Flow
- ? AI turn esegue con delay corretto
- ? Carta AI viene mostrata prima della cattura
- ? GameState si aggiorna correttamente
- ? Eventi OnMoveExecuted vengono fired

### 4. Accusi UI
- ? Badge "Decino" appare su captured pile
- ? Scope cards sono visualizzate separatamente
- ? Hover su scope pile espande le carte

## ?? Troubleshooting

### Problema: `NUnit.Framework` not found

**Soluzione**: Verifica che `.asmdef` abbia:
```json
"overrideReferences": true,
"precompiledReferences": ["nunit.framework.dll"]
```

### Problema: `Project51.Core` not found

**Soluzione**: 
1. Verifica che `Project51.Core.asmdef` esista in `Assets/Scripts/Core/`
2. Aggiungi riferimento nell'asmdef dei test:
```json
"references": ["Project51.Core", "Project51.Gameplay"]
```
3. **Reimport All** in Unity

### Problema: Test non appaiono nel Test Runner

**Soluzione**:
1. Verifica che `.asmdef` abbia: `"defineConstraints": ["UNITY_INCLUDE_TESTS"]`
2. Verifica che namespace sia corretto: `namespace Project51.PlayModeTests`
3. Refresh Unity (Ctrl+R)

## ?? Differenze: Editor vs PlayMode Tests

| Caratteristica | Editor Tests | PlayMode Tests |
|---|---|---|
| **Velocità** | ? Velocissimi (~ms) | ?? Lenti (~secondi) |
| **MonoBehaviour** | ? No lifecycle | ? Start(), Update(), etc. |
| **Coroutines** | ? Non supportate | ? Supportate |
| **UI Interaction** | ? No click/drag | ? Simulabile |
| **Quando Usare** | Logica pura, regole | Integrazione, UI, timing |

## ?? Best Practices

1. **Mantieni Setup Semplice**: Crea solo componenti necessari
2. **Usa `yield return null`**: Aspetta un frame dopo Setup
3. **Cleanup Sempre**: Distruggi GameObjects in `TearDown`
4. **Timeout Ragionevoli**: `yield return new WaitForSeconds(0.3f)` è sufficiente
5. **Test Indipendenti**: Ogni test dovrebbe funzionare da solo

## ?? Risorse

- [Unity Test Framework Documentation](https://docs.unity3d.com/Packages/com.unity.test-framework@latest)
- [NUnit Framework](https://nunit.org/)
- [Assembly Definitions](https://docs.unity3d.com/Manual/ScriptCompilationAssemblyDefinitionFiles.html)

---

**Nota**: I PlayMode tests sono più lenti ma **essenziali** per catturare bug che gli Editor tests non possono vedere (timing, coroutine, UI lifecycle).
