// app.js - Initializes routing and handles dynamic module loading

(function() {
    'use strict';

    // Get the content container
    const contentContainer = document.getElementById('content');

    /**
     * Renders the welcome/home page
     */
    function renderWelcome() {
        contentContainer.innerHTML = `
            <div class="welcome-section">
                <h1>Welcome to MainApp</h1>
                <p class="lead">A modular architecture demonstration with ASP.NET Core Web API and dynamic frontend modules.</p>
                
                <div class="row mt-4">
                    <div class="card mb-3">
                        <div class="card-body">
                            <h5 class="card-title">🚀 Dynamic Modules</h5>
                            <p class="card-text">Load modules dynamically at runtime without page refresh.</p>
                            <a href="/modules/exampleModule" class="btn btn-primary">Try Example Module</a>
                        </div>
                    </div>
                    <div class="card mb-3">
                        <div class="card-body">
                            <h5 class="card-title">🔌 REST API</h5>
                            <p class="card-text">Access powerful REST API endpoints for module management.</p>
                            <a href="/scalar/v1" target="_blank" class="btn btn-primary">View API Docs</a>
                        </div>
                    </div>
                    <div class="card mb-3">
                        <div class="card-body">
                            <h5 class="card-title">⚡ Real-time Updates</h5>
                            <p class="card-text">Manage modules with real-time status and health monitoring.</p>
                            <button class="btn btn-primary" id="learnMoreBtn">Learn More</button>
                        </div>
                    </div>
                </div>

                <div class="mt-4">
                    <h3>Features</h3>
                    <ul class="list-group">
                        <li class="list-group-item">✅ Bootstrap-style responsive layout</li>
                        <li class="list-group-item">✅ Client-side routing with custom router</li>
                        <li class="list-group-item">✅ Dynamic module loading</li>
                        <li class="list-group-item">✅ RESTful API integration</li>
                        <li class="list-group-item">✅ Modular frontend architecture</li>
                    </ul>
                </div>
            </div>
        `;
        
        // Add event listener for Learn More button
        const learnMoreBtn = contentContainer.querySelector('#learnMoreBtn');
        if (learnMoreBtn) {
            learnMoreBtn.addEventListener('click', () => {
                alert('Feature coming soon!');
            });
        }
    }

    /**
     * Loads and renders a module dynamically
     * @param {string} moduleName - The name of the module to load
     */
    async function loadModule(moduleName) {
        contentContainer.innerHTML = `
            <div class="text-center">
                <div class="spinner-border"></div>
                <p>Loading module...</p>
            </div>
        `;

        try {
            // Dynamically import the module
            const moduleUrl = `/modules/${moduleName}.js`;
            const module = await import(moduleUrl);
            
            // Check if the module has a render function
            if (typeof module.render === 'function') {
                // Clear the container
                contentContainer.innerHTML = '';
                
                // Create a wrapper for the module content
                const moduleWrapper = document.createElement('div');
                moduleWrapper.className = 'module-content';
                contentContainer.appendChild(moduleWrapper);
                
                // Call the module's render function
                await module.render(moduleWrapper);
            } else {
                throw new Error(`Module "${moduleName}" does not export a render function`);
            }
        } catch (error) {
            console.error('Error loading module:', error);
            contentContainer.innerHTML = `
                <div class="alert alert-danger">
                    <h4>⚠️ Error Loading Module</h4>
                    <p>Failed to load module: <strong>${moduleName}</strong></p>
                    <p>Error: ${error.message}</p>
                    <a href="/" class="btn btn-primary mt-3">← Back to Home</a>
                </div>
            `;
        }
    }

    /**
     * Initialize routing with custom router
     */
    function initializeRouting() {
        // Home route
        router.route('/', () => {
            renderWelcome();
        });

        // Module route - dynamically loads module JS
        router.route('/modules/:module', (params) => {
            const moduleName = params.module;
            loadModule(moduleName);
        });

        // 404 route
        router.route('*', () => {
            contentContainer.innerHTML = `
                <div class="alert alert-danger">
                    <h4>404 - Page Not Found</h4>
                    <p>The requested page could not be found.</p>
                    <a href="/" class="btn btn-primary">← Back to Home</a>
                </div>
            `;
        });

        // Start the router
        router.start();
    }

    // Initialize when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initializeRouting);
    } else {
        initializeRouting();
    }

    // Expose utility functions globally
    window.MainApp = {
        navigateTo: function(path) {
            router.navigate(path);
        },
        reloadModule: function() {
            // Get current path and reload if it's a module
            const currentPath = location.pathname;
            const moduleMatch = currentPath.match(/^\/modules\/(.+)$/);
            if (moduleMatch) {
                loadModule(moduleMatch[1]);
            } else {
                window.location.reload();
            }
        }
    };
})();
