// exampleModule.js - Example dynamic module that demonstrates the module pattern

/**
 * Renders the example module content
 * @param {HTMLElement} container - The container element to render the module into
 */
export async function render(container) {
    // Create the module UI
    container.innerHTML = `
        <div>
            <div class="module-header">
                <h2>📦 Example Module</h2>
                <p class="text-muted">Dynamically loaded module demonstration</p>
            </div>

            <div class="row">
                <div style="grid-column: span 2;">
                    <div class="card">
                        <h3>About This Module</h3>
                        <p>This is an example module that is dynamically loaded from <code>/modules/exampleModule.js</code>. 
                        It demonstrates how modules can be loaded on-demand without reloading the entire page.</p>
                    </div>

                    <div class="card">
                        <h3>Module Features</h3>
                        <ul>
                            <li>Dynamic loading via ES6 modules</li>
                            <li>Independent rendering logic</li>
                            <li>Can interact with API endpoints</li>
                            <li>Bootstrap styling support</li>
                            <li>Event handling and state management</li>
                        </ul>
                    </div>

                    <div class="card">
                        <div style="background-color: var(--bs-primary); color: white; padding: 0.75rem; margin: -1.5rem -1.5rem 1rem; border-radius: 0.375rem 0.375rem 0 0;">
                            <strong>Interactive Demo</strong>
                        </div>
                        <p>Click the button below to fetch module information from the API:</p>
                        <button id="fetchModulesBtn" class="btn btn-primary">
                            Fetch Modules
                        </button>
                        <div id="moduleResults" class="mt-3"></div>
                    </div>

                    <div class="card">
                        <div style="background-color: var(--bs-success); color: white; padding: 0.75rem; margin: -1.5rem -1.5rem 1rem; border-radius: 0.375rem 0.375rem 0 0;">
                            <strong>Counter Demo</strong>
                        </div>
                        <p>Test module state management:</p>
                        <div class="d-flex align-items-center gap-3">
                            <button id="decrementBtn" class="btn btn-secondary">-</button>
                            <h3 style="margin: 0;" id="counterValue">0</h3>
                            <button id="incrementBtn" class="btn btn-secondary">+</button>
                            <button id="resetBtn" class="btn btn-outline-danger">Reset</button>
                        </div>
                    </div>
                </div>

                <div>
                    <div class="card">
                        <div style="background-color: var(--bs-info); color: white; padding: 0.75rem; margin: -1.5rem -1.5rem 1rem; border-radius: 0.375rem 0.375rem 0 0;">
                            <strong>Module Info</strong>
                        </div>
                        <dl>
                            <dt>Module Name:</dt>
                            <dd>exampleModule</dd>
                            
                            <dt>Version:</dt>
                            <dd>1.0.0</dd>
                            
                            <dt>Type:</dt>
                            <dd>ES6 Module</dd>
                            
                            <dt>Loaded:</dt>
                            <dd id="loadTime">-</dd>
                        </dl>
                    </div>

                    <div class="alert alert-info" role="alert">
                        <strong>💡 Tip:</strong> You can create more modules by adding new JS files to the <code>/modules/</code> folder and accessing them via <code>/modules/yourModule</code>
                    </div>
                </div>
            </div>

            <div class="mt-4">
                <a href="/" class="btn btn-secondary">← Back to Home</a>
                <button id="reloadModuleBtn" class="btn btn-primary">🔄 Reload Module</button>
            </div>
        </div>
    `;

    // Set load time
    const loadTimeElement = container.querySelector('#loadTime');
    loadTimeElement.textContent = new Date().toLocaleTimeString();

    // Initialize module functionality
    initializeCounterDemo(container);
    initializeFetchDemo(container);
    initializeModuleControls(container);
}

/**
 * Initializes the counter demo functionality
 */
function initializeCounterDemo(container) {
    let counter = 0;
    const counterValue = container.querySelector('#counterValue');
    const incrementBtn = container.querySelector('#incrementBtn');
    const decrementBtn = container.querySelector('#decrementBtn');
    const resetBtn = container.querySelector('#resetBtn');

    const updateCounter = (value) => {
        counter = value;
        counterValue.textContent = counter;
        counterValue.style.color = counter > 0 ? 'green' : counter < 0 ? 'red' : 'black';
    };

    incrementBtn.addEventListener('click', () => updateCounter(counter + 1));
    decrementBtn.addEventListener('click', () => updateCounter(counter - 1));
    resetBtn.addEventListener('click', () => updateCounter(0));
}

/**
 * Initializes the API fetch demo
 */
function initializeFetchDemo(container) {
    const fetchBtn = container.querySelector('#fetchModulesBtn');
    const resultsDiv = container.querySelector('#moduleResults');

    fetchBtn.addEventListener('click', async () => {
        fetchBtn.disabled = true;
        fetchBtn.textContent = 'Loading...';
        resultsDiv.innerHTML = '';

        try {
            const response = await fetch('/api/modules');
            
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }
            
            const modules = await response.json();
            
            if (modules.length === 0) {
                resultsDiv.innerHTML = `
                    <div class="alert alert-info">
                        No modules are currently running. Use the API to start modules.
                    </div>
                `;
            } else {
                let html = '<div class="list-group">';
                modules.forEach(module => {
                    const statusClass = module.status === 'Running' ? 'bg-success' : 'bg-secondary';
                    html += `
                        <div class="list-group-item">
                            <div class="d-flex justify-content-between align-items-center">
                                <strong>${module.name}</strong>
                                <span class="badge ${statusClass}">${module.status}</span>
                            </div>
                            <small class="text-muted">Port: ${module.port} | PID: ${module.processId || 'N/A'}</small>
                        </div>
                    `;
                });
                html += '</div>';
                resultsDiv.innerHTML = html;
            }
        } catch (error) {
            resultsDiv.innerHTML = `
                <div class="alert alert-danger">
                    <strong>Error:</strong> ${error.message}
                </div>
            `;
        } finally {
            fetchBtn.disabled = false;
            fetchBtn.textContent = 'Fetch Modules';
        }
    });
}

/**
 * Initializes module control buttons
 */
function initializeModuleControls(container) {
    const reloadBtn = container.querySelector('#reloadModuleBtn');
    
    reloadBtn.addEventListener('click', () => {
        if (window.MainApp && typeof window.MainApp.reloadModule === 'function') {
            window.MainApp.reloadModule();
        } else {
            window.location.reload();
        }
    });
}

// Export additional module metadata if needed
export const metadata = {
    name: 'exampleModule',
    version: '1.0.0',
    description: 'Example module demonstrating dynamic loading and rendering',
    author: 'MainApp Team'
};
