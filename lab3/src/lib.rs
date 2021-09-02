use log::{error,info};
use proxy_wasm::{
    traits::{Context,HttpContext,RootContext},
    types::{Action,ContextType,LogLevel}};
use serde::Deserialize;

const PATH: &str = ":path";
const HEADER_NAME: &str = "X-Wasm-Path";

// root handler holds config
struct RootHandler {
    config: FilterConfig,
}

// each request gets header and path from root
pub struct RequestContext {
    add_header: bool,
    header: String,
    path: String,
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

    info!("starting lab 3");

    // create root context and load config
    proxy_wasm::set_root_context(|_context_id| -> Box<dyn RootContext> {
        Box::new(RootHandler { config: FilterConfig::default() })
    });
}

// Root Context implementation

impl Context for RootHandler {}

impl RootContext for RootHandler {
    // create http context for new requests
    fn create_http_context(&self, _context_id: u32) -> Option<Box<dyn HttpContext>> {
        Some(Box::new(RequestContext {
            add_header: false,
            // copy the values from root config to request
            path: self.config.path.clone(),
            header: self.config.burst_header.clone(),
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
                        self.config = config;
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

    // check headers for path match and store in self
    fn on_http_request_headers(&mut self, _: usize) -> Action {

        if self.get_http_request_header(PATH).unwrap_or_default() == self.path {
            self.add_header = true;
        }

        Action::Continue
    }
    
    // add the header if path matched
    fn on_http_response_headers(&mut self, _num_headers: usize) -> Action {

        if self.add_header {
            self.set_http_response_header(HEADER_NAME,Some(&self.header));
        }

        Action::Continue
    }
}
