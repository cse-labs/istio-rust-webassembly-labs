use log::{error,info};
use proxy_wasm::{
    traits::{Context,HttpContext,RootContext},
    types::{Action,ContextType,LogLevel}};
use serde::Deserialize;

const HEADER_NAME: &str = "X-Wasm-Config";

// root handler holds config
struct RootHandler {
    header: String,
}

// each request gets header from root
pub struct RequestContext {
    header: String,
}

// config structure
#[derive(Deserialize, Debug)]
#[serde(default)]
pub struct FilterConfig {
    /// current burst header - gets updated on time
    burst_header: String,

    /// Cache duration in seconds
    cache_seconds: u64,

    /// Name of this deployment
    deployment: String,

    /// Namespace of this app
    namespace: String,

    /// The authority to set when calling the HTTP service providing headers.
    service_authority: String,

    /// The Envoy cluster name
    service_cluster: String,

    /// The path to call on the HTTP service providing headers.
    service_path: String,

    /// path
    path: String,
}

// default values for config
impl Default for FilterConfig {
    fn default() -> Self {
        FilterConfig {
            burst_header: String::new(),
            cache_seconds: 60 * 60 * 24,
            deployment: String::new(),
            namespace: String::new(),
            service_authority: String::new(),
            service_cluster: String::new(),
            service_path: String::new(),
            path: String::new(),
        }
    }
}

#[no_mangle]
pub fn _start() {
    proxy_wasm::set_log_level(LogLevel::Warn);

    info!("starting lab 2");

    // create root context and load config
    proxy_wasm::set_root_context(|_context_id| -> Box<dyn RootContext> {
        Box::new(RootHandler { header: String::new() })
    });
}

// Root Context implementation

impl Context for RootHandler {}

impl RootContext for RootHandler {
    // create http context for new requests
    fn create_http_context(&self, _context_id: u32) -> Option<Box<dyn HttpContext>> {
        Some(Box::new(RequestContext {
            header: self.header.clone(),
        }))
    }

    // required for create_http_context to work
    fn get_type(&self) -> Option<ContextType> {
        Some(ContextType::HttpContext)
    }

    // read the config and store in self.config
    fn on_configure(&mut self, _config_size: usize) -> bool {
        match self.get_configuration() {
            Some(c) => {
                // Parse and store the configuration
                match serde_json::from_slice::<FilterConfig>(c.as_ref()) {
                    Ok(config) => {
                        self.header = config.burst_header.clone();
                    }
                    Err(e) => {
                        // fail on invalid config
                        error!("failed to parse configuration: {:?}", e);
                        return false;
                    }
                }
            }
            None => {
                // fail on missing config
                error!("configuration missing");
                return false;
            }
        };

        return true;
    }
}

// http request implemenetation

// nothing implemented
impl Context for RequestContext {}

impl HttpContext for RequestContext {

    // add the header if path matched
    fn on_http_response_headers(&mut self, _num_headers: usize) -> Action {

        self.set_http_response_header(HEADER_NAME,Some(&self.header));

        Action::Continue
    }
}
