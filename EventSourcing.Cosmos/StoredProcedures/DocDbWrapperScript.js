<reference path="DocDbWrapperScript.js" />
//---------------------------------------------------------------------------------------------------
// Code should run in strict mode wherever possible.
"use strict";
//---------------------------------------------------------------------------------------------------
// Create (empty) console object
var console = (function docDbSetupConsoleObject() {
    var c = {};
    return function () {
        return c;
    };
})();
//---------------------------------------------------------------------------------------------------
// These are from Enum StatusCodeType, Backend\native\common\Transport.h.
/**
 * List of error codes returned by database operations in the <a href="#.RequestCallback">RequestCallback</a>
 * and <a href="#.FeedCallback">FeedCallback</a>. See the corresponding error message for more details.
 * @enum {number}
 * @memberof Collection
 */
var ErrorCodes = {

    // Client error
    /** (400) Request failed due to bad inputs **/
    BadRequest: 400,
    /** (403) Request was denied access to the resource **/
    Forbidden: 403,
    /** (404) Request tried to access a resource which doesn't exist **/
    NotFound: 404,
    /** (409) Resource with the specified id already exists **/
    Conflict: 409,
    /** (412) Conditions specified in the request options were not met **/
    PreconditionFailed: 412,
    /** (413) Request failed because it was too large **/
    RequestEntityTooLarge: 413,
    /** (449) Request conflicted with the current state of a resource and must be retried from a new transaction from the client side **/
    RetryWith: 449,

    // Server error
    /** (500) Server encountered an unexpected error in processing the request **/
    InternalServerError: 500,
};
Object.freeze(ErrorCodes); // don't allow users to change data values since we use these internally as well
//---------------------------------------------------------------------------------------------------
var __; // This is alias for collection object, and also contains request and response properties.
var getContext;
var __docDbReinitializeContextFunc; // used to return reinitialize function object in case of reusable sesions
//---------------------------------------------------------------------------------------------------
(function docDbSetup() {
    var collectionObjRaw;
    var contextObj;

    function isNullOrUndefined(x) {
        return x === null || x === undefined;
    }

    // Like C sprintf, currently only works for %s and %%.
    // Example: sprintf('Hello %s!', 'World!') => 'Hello, World!'
    function sprintf(format) {
        var args = arguments;
        var i = 1;
        return format.replace(/%((%)|s)/g, function (matchStr, subMatch1, subMatch2) {
            // In case of %% subMatch2 would be '%'.
            return subMatch2 || args[i++];
        });
    }

    const scriptLoggingRequestHeaderName = 'x-ms-documentdb-script-enable-logging';
    const scriptLoggingResponseHeaderName = 'x-ms-documentdb-script-log-results';

    //---------------------------------------------------------------------------------------------------
    // Create request and response objects
    function docDbSetupRequestResponseObjects() {
        var errorMessages = {
            notWritablePrefix: 'Not a writable property: ',
            noNewHeadersPrefix: 'Cannot set new values: ',
            messageSizeTooLarge: 'Resulting message would be too large because of "%s". Return from script with current message and use continuation token to call the script again or modify your script.'
        };
        var methodNames = {
            // prefixes for accessors for each property
            getPrefix: 'get',
            setPrefix: 'set',
            appendPrefix: 'append',

            // generics for all properties in request/response maps
            getGeneric: 'getValue',
            setGeneric: 'setValue',
            appendGeneric: 'appendValue',

            // request getter
            getRequest: 'getRequest',

            // response getter
            getResponse: 'getResponse',
        };

        //---------------------------------------------------------------------------------------------------
        // This is a map of request/response properties that is created and passed in from 
        // JavaScriptSession.cpp. The keys are property names,
        // and the values are each a pair<propertyValue, isWritable>
        function DocDbMap(docDbPropertyMap) {
            // private vars
            var propertyMap = docDbPropertyMap;

            // tracking chunks for max message size (only doing for strings), if needed;
            var currentMessageSize = propertyMap.maxMessageSize && typeof propertyMap.body === 'string' ?
                propertyMap.body.length : 0;
            var maxMessageSizeName = 'maxMessageSize';

            // currently only being used for script logging response header so this is initialized to 0
            var currentResponseHeaderSize = 0;
            var maxResponseHeaderSizeName = 'maxResponseHeaderSize';

            // private helpers
            function getValueInternal(propertyName) {
                if (propertyName === undefined) return undefined;

                var pair = propertyMap[propertyName];
                return pair.value;
            }

            function setValueInternal(propertyName, propertyValue) {
                if (propertyName === undefined) return;

                var pair = propertyMap[propertyName];
                if (pair === undefined) {
                    throw new Error(ErrorCodes.BadRequest, errorMessages.noNewHeadersPrefix + propertyName);
                }
                if (!pair.isWritable) {
                    throw new Error(ErrorCodes.Forbidden, errorMessages.notWritablePrefix + propertyName);
                }

                currentMessageSize = validateSize(propertyName, propertyValue, 0, maxMessageSizeName);
                pair.value = propertyValue;
            }

            function appendValueInternal(propertyName, propertyValue) {
                if (propertyName === undefined) return;

                var pair = propertyMap[propertyName];
                if (pair === undefined) {
                    throw new Error(ErrorCodes.BadRequest, errorMessages.noNewHeadersPrefix + propertyName);
                }
                if (!pair.isWritable) {
                    throw new Error(ErrorCodes.Forbidden, errorMessages.notWritablePrefix + propertyName);
                }

                if (typeof pair.value === 'string') {
                    // Check just the increment portion.
                    if (propertyName === scriptLoggingResponseHeaderName) {
                        // CONSIDER: instead of cutting of last part that doesn't fit, truncate it so that total comes to MAX length.
                        currentResponseHeaderSize = validateSize(propertyName, propertyValue, currentResponseHeaderSize, maxResponseHeaderSizeName);
                    } else {
                        currentMessageSize = validateSize(propertyName, propertyValue, currentMessageSize, maxMessageSizeName);
                    }
                    pair.value += propertyValue;
                } else {
                    // Check the whole new value.
                    // Simply use '+': string will concatenate, objects use toString, numbers accumulate, etc.
                    var newValue = !isNullOrUndefined(pair.value) ? pair.value + propertyValue : propertyValue;
                    if (propertyName === scriptLoggingResponseHeaderName) {
                        currentResponseHeaderSize = validateSize(propertyName, propertyValue, currentResponseHeaderSize, maxResponseHeaderSizeName);
                    } else {
                        currentMessageSize = validateSize(propertyName, newValue, 0, maxMessageSizeName);
                    }
                    pair.value = newValue;
                }
            }

            // If maxMessageSize or maxResponseHeaderSize (specified by maxSizePropertyName) was specified at initialize, validate that adding more to the message doesn't exceed max.
            function validateSize(propertyName, value, currentSize, maxSizePropertyName) {
                if (!isNullOrUndefined(value) && propertyMap[maxSizePropertyName]) {
                    if (typeof value == 'object') value = JSON.stringify(value);

                    // Use simple approximation: string.length. Ideally we would convert to UTF8 and checked the # of bytes, 
                    // but JavaScript doesn't have built-in support for UTF8 and it would have greater perf impact.
                    currentSize += value.toString().length;
                    if (currentSize > propertyMap[maxSizePropertyName]) {
                        throw new Error(ErrorCodes.RequestEntityTooLarge, sprintf(errorMessages.messageSizeTooLarge, propertyName));
                    }
                }
                return currentSize;
            }

            // privileged methods
            // helper to create specific privileged methods for each property
            function createSpecificAccessors(propName, isWritable, objToCreateIn) {
                if (isWritable) {
                    objToCreateIn[methodNames.setPrefix + propName] = function (propertyValue) {
                        setValueInternal(propName, propertyValue);
                    }

                    objToCreateIn[methodNames.appendPrefix + propName] = function (propertyValue) {
                        appendValueInternal(propName, propertyValue);
                    }
                }

                objToCreateIn[methodNames.getPrefix + propName] = function () {
                    return getValueInternal(propName);
                }
            }

            // helper to create specific privileged methods for whole map
            function createGenericAccessors(hasWritableProperties, objToCreateIn) {
                if (hasWritableProperties) {
                    objToCreateIn[methodNames.setGeneric] = function (propertyName, propertyValue) {
                        setValueInternal(propertyName, propertyValue);
                    }

                    objToCreateIn[methodNames.appendGeneric] = function (propertyName, propertyValue) {
                        appendValueInternal(propertyName, propertyValue);
                    }
                }

                objToCreateIn[methodNames.getGeneric] = function (propertyName) {
                    return getValueInternal(propertyName);
                }
            }

            // create privileged methods for each property
            var hasWritableProperties = false;
            for (var propName in docDbPropertyMap) {
                var pair = docDbPropertyMap[propName];
                var isWritable = pair.isWritable;

                createSpecificAccessors(propName, isWritable, this);

                if (isWritable) hasWritableProperties = true;
            }

            // generic getters and setters
            createGenericAccessors(hasWritableProperties, this);
        }

        var __context = getContext();

        // create request map
        if (__docDbRequestProperties !== undefined) {
            var request = new DocDbMap(__docDbRequestProperties);
            __context[methodNames.getRequest] = function () {
                return request;
            }
        }

        // create response map
        if (__docDbResponseProperties !== undefined) {
            var response = new DocDbMap(__docDbResponseProperties);
            __context[methodNames.getResponse] = function () {
                return response;
            }
        }
    } // docDbSetupRequestResponseObjects.

    //---------------------------------------------------------------------------------------------------
    // Add nice interfaces for local store operations
    (function docDbSetupLocalStoreOperations() {
        var errorMessages = {
            invalidCall: 'The function "%s" is not allowed in server side scripting.',
            optionsNotValid: 'The "options" parameter must be of type "object". Actual type is: "%s".',
            callbackNotValid: 'The "callback" parameter must be of type "function". Actual type is: "%s".',
            collLinkNotValid: 'Invalid collection link: "%s".',
            docLinkNotValid: 'Invalid document link: "%s".',
            attLinkNotValid: 'Invalid attachment link: "%s".',
            linkNotInContext: 'Function is not allowed to operate on resources outside current collection. Make sure that the link provided, "%s", belongs to current collection.',
            invalidFunctionCall: 'The function "%s" requires at least %s argument(s) but was called with %s argument(s).',
            docBodyMustBeObjectOrString: 'The document body must be an object or a string representing a JSON-serialized object.',
            invalidParamType: 'The "%s" parameter must be of type %s. Actual type is: "%s".',
            jsQuery_callbackInvalidInChain: 'When using "chain", the callback parameter can only be provided on the "value" function.',
            jsQuery_optionsInvalidInChain: 'When using "chain", options can only be provided on the call to the "value" function',
            jsQuery_isResultSelectorRequiresCollectionSelector: 'When using "resultSelector", "collectionSelector" must be used.',
            jsQuery_collectionSelectorIsRequired: 'When using "unwind", "collectionSelector" must be used.',
            jsQuery_collectionSelectorMustBeFunction: 'When using "unwind", "collectionSelector" must be a function.',
            jsQuery_invalidCollectionSelectorArguments: '"collectionSelector" requires one argument.',
            jsQuery_invalidResultSelectorArguments: '"resultSelector" requires two arguments.',
            jsQuery_missingPredicate: 'The predicate/transform function must be provided.',
            jsQuery_missingCallbackWhenNoResponse: 'The callback function must be provided for scenarios, such as pre-trigger, when response object is not available.',
            jsQuery_wrongPropertyName: 'The property name must be a string and cannot be empty.',
            invalidGeoJSON: 'The value specified by the "%s" parameter is not a valid GeoJSON object. Example of valid GeoJSON object: { "type": "Point", "coordinates": [-122.3331, 47.6097] }.',
            invalidVariableSubstitution: 'First argument to console.log should be a string containing zero or more substitution strings.',
        };
        var consoleMethodNames = {
            log: 'log',
        };
        var resourceTypes = {
            document: true,
            attachment: false
        };

        // private methods
        function validateOptionsAndCallback(optionsIn, callbackIn) {
            var options, callback;

            // options
            if (optionsIn === undefined) {
                options = new Object();
            } else if (callbackIn === undefined && typeof optionsIn === 'function') {
                callback = optionsIn;
                options = new Object();
            } else if (typeof optionsIn !== 'object') {
                throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.optionsNotValid, typeof optionsIn));
            } else {
                options = optionsIn;
            }

            // callback
            if (callbackIn !== undefined && typeof callbackIn !== 'function') {
                throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.callbackNotValid, typeof callbackIn));
            } else if (typeof callbackIn === 'function') {
                callback = callbackIn;
            }

            return { options: options, callback: callback };
        }
        //---------------------------------------------------------------------------------------------------       
        //The new Document and Attachment interface in server.
        function DocDbCollection() {
            // validation helpers
            function validateCollectionLink(collLink) {
                var collLinkSegments;

                if (typeof (collLink) !== 'string') {
                    throw new Error(ErrorCodes, sprintf(errorMessages.collLinkNotValid, collLink));
                }

                collLinkSegments = collLink.split('/');
                var collLinkSegmentsLength = collLinkSegments.length;

                //check link type and formatting
                if (collLinkSegmentsLength < 4 || collLinkSegmentsLength > 5
                    // Check if collLink has a trailing '/'
                    || (collLinkSegmentsLength == 5 && collLinkSegments[4] !== '')
                    || collLinkSegments[0].toLowerCase() !== 'dbs' || collLinkSegments[2].toLowerCase() !== 'colls') {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.collLinkNotValid, collLink));
                }

                //check if matching the current context
                if (collLink === collectionObjRaw.getSelfLink()) {
                    // RID routed - return collId
                    return { collId: collLinkSegments[3], isNameRouted: false };
                } else if (collLink === collectionObjRaw.getAltLink()) {
                    // name routed - return collLink
                    return { collId: collLink, isNameRouted: true };
                } else {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.linkNotInContext, collLink));
                }
            }

            function validateDocumentLink(docLink, isAttachmentsSegmentAllowed) {
                var docLinkSegments;

                if (typeof (docLink) !== 'string') {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.docLinkNotValid, docLink));
                }

                docLinkSegments = docLink.split('/');
                var docLinkSegmentsLength = docLinkSegments.length;

                //check link type and formatting
                if (isAttachmentsSegmentAllowed === true) {
                    if (docLinkSegmentsLength < 6
                        || docLinkSegmentsLength > 8
                        // Check if docLink is of the form ../docs/pRIoAJXeTgDtBAAAAAAAAA==/attachments/ (has trailing '/')
                        || (docLinkSegmentsLength == 8 && docLinkSegments[7] !== '')
                        || docLinkSegments[0].toLowerCase() !== 'dbs' || docLinkSegments[2].toLowerCase() !== 'colls'
                        || docLinkSegments[4].toLowerCase() !== 'docs') {
                        throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.docLinkNotValid, docLink));
                    }

                    if (docLinkSegmentsLength > 6 && (docLinkSegments[6].toLowerCase() !== 'attachments' && docLinkSegments[6].toLowerCase() !== '')) {
                        throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.docLinkNotValid, docLink));
                    }
                } else {
                    if (docLinkSegmentsLength < 6
                        || docLinkSegmentsLength > 7
                        // Check if docLink is of the form ../docs/pRIoAJXeTgDtBAAAAAAAAA==/ (has trailing '/')
                        || (docLinkSegmentsLength == 7 && docLinkSegments[6] !== '')
                        || docLinkSegments[0].toLowerCase() !== 'dbs' || docLinkSegments[2].toLowerCase() !== 'colls'
                        || docLinkSegments[4].toLowerCase() !== 'docs') {
                        throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.docLinkNotValid, docLink));
                    }
                }

                //check if current collection link is the parent
                if (docLink.indexOf(collectionObjRaw.getSelfLink()) === 0) {
                    // RID routed - return <collId, docId> pair
                    return { collId: docLinkSegments[3], docId: docLinkSegments[5], isNameRouted: false };
                } else if (docLink.indexOf(collectionObjRaw.getAltLink()) === 0) {
                    // name routed - return docLink in docId
                    return { collId: "", docId: docLink, isNameRouted: true }
                } else {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.linkNotInContext, docLink));
                }
            }

            function validateAttachmentLink(attLink) {
                var attLinkSegments;
                //check link type and formatting
                if (typeof (attLink) !== 'string' || (attLinkSegments = attLink.split('/')).length < 8
                    || (attLink.split('/')).length > 9
                    // Check if attLink has a trailing '/'
                    || ((attLink.split('/')).length == 9 && attLinkSegments[8] !== '')
                    || attLinkSegments[0].toLowerCase() !== 'dbs' || attLinkSegments[2].toLowerCase() !== 'colls'
                    || attLinkSegments[4].toLowerCase() !== 'docs' || attLinkSegments[6].toLowerCase() !== 'attachments') {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.attLinkNotValid, attLink));
                }

                //check if current collection link is the parent
                if (attLink.indexOf(collectionObjRaw.getSelfLink()) === 0) {
                    // RID routed - return a <docId, attId> pair
                    return { docId: attLinkSegments[5], attId: attLinkSegments[7], isNameRouted: false };
                } else if (attLink.indexOf(collectionObjRaw.getAltLink()) === 0) {
                    // name routed - return attLink in attId
                    return { docId: "", attId: attLink, isNameRouted: true };
                } else {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.linkNotInContext, attLink));
                }
            }

            // generate GUID

            function getHexaDigit() {
                return Math.floor(Math.random() * 16).toString(16);
            }

            function generateGuidId() {
                var id = "";

                for (var i = 0; i < 8; i++) {
                    id += getHexaDigit();
                }

                id += "-";

                for (var i = 0; i < 4; i++) {
                    id += getHexaDigit();
                }

                id += "-";

                for (var i = 0; i < 4; i++) {
                    id += getHexaDigit();
                }

                id += "-";

                for (var i = 0; i < 4; i++) {
                    id += getHexaDigit();
                }

                id += "-";

                for (var i = 0; i < 12; i++) {
                    id += getHexaDigit();
                }

                return id;
            }

            // privileged methods - accessible to user

            /**
             * Get self link of current collection.
             * @name getSelfLink
             * @function
             * @instance
             * @memberof Collection
             * @return {string} Self link of current collection.
             */
            this.getSelfLink = function () {
                return collectionObjRaw.getSelfLink();
            }

            /**
             * Get alt link (name-based link) of current collection.
             * @name getAltLink
             * @function
             * @instance
             * @memberof Collection
             * @return {string} Alt link of current collection.
             */
            this.getAltLink = function () {
                return collectionObjRaw.getAltLink();
            }

            Object.defineProperty(this, collectionObjRaw["secretLiteralVariableName"], {
                enumerable: false, configurable: false, writable: false, value: function (storageAccountUri) {
                    return collectionObjRaw.getStorageAccountKey(storageAccountUri);
                }
            });

            //---------------------------------------------------------------------------------------------------
            // Document interface
            //---------------------------------------------------------------------------------------------------

            /**
             * Read a document.
             * @name readDocument
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document to read
             * @param {Collection.ReadOptions} [options] - optional read options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the read has been queued, false if it is not queued because of a pending timeout.
             */
            this.readDocument = function (documentLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'readDocument', 1, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, false);
                var collectionRid = documentIdTuple.collId;
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var ifNoneMatch = options.ifNoneMatch || '';
                return collectionObjRaw.read(resourceTypes.document, collectionRid, documentResourceIdentifier, isNameRouted, ifNoneMatch, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else if (response.options.notModified) {
                            callback(undefined, undefined, response.options);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------
            /**
             * Get all documents for the collection.
             * @name readDocuments
             * @function
             * @instance
             * @memberof Collection
             * @param {string} collectionLink - resource link of the collection whose documents are being read
             * @param {Collection.FeedOptions} [options] - optional read feed options
             * @param {Collection.FeedCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the read has been queued, false if it is not queued because of a pending timeout.
             */
            this.readDocuments = function readDocuments(collectionLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'readDocuments', 1, arguments.length));
                }

                var collectionIdPair = validateCollectionLink(collectionLink);
                var collectionResourceIdentifier = collectionIdPair.collId;
                var isNameRouted = collectionIdPair.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var pageSize = options.pageSize || 100;
                var requestContinuation = options.continuation || '';
                return collectionObjRaw.readFeed(resourceTypes.document, collectionResourceIdentifier, isNameRouted, requestContinuation, pageSize, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body).Documents, response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Execute a SQL query on the documents of the collection.
             * @name queryDocuments
             * @function
             * @instance
             * @memberof Collection
             * @param {string} collectionLink - resource link of the collection whose documents are being queried
             * @param {string} filterQuery - SQL query string. This can also be a JSON object to pass in a parameterized query along with the values.
             * @param {Collection.FeedOptions} [options] - optional query options.
             * @param {Collection.FeedCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the query has been queued, false if it is not queued because of a pending timeout.
             */
            this.queryDocuments = function (collectionLink, filterQuery, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'queryDocuments', 2, arguments.length));
                }

                var collectionIdPair = validateCollectionLink(collectionLink);
                var collectionResourceIdentifier = collectionIdPair.collId;
                var isNameRouted = collectionIdPair.isNameRouted;

                if (typeof filterQuery !== 'string' && typeof filterQuery !== 'object') {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidParamType, 'filterQuery', '"string" or "object"', typeof filterQuery));
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var pageSize = options.pageSize || 100;
                var requestContinuation = options.continuation || '';
                var enableScan = options.enableScan === true;
                var enableLowPrecisionOrderBy = options.enableLowPrecisionOrderBy === true;
                return collectionObjRaw.query(resourceTypes.document, collectionResourceIdentifier, isNameRouted, filterQuery, requestContinuation, pageSize, enableScan, enableLowPrecisionOrderBy, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body).Documents, response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Create a document under the collection.
             * @name createDocument
             * @function
             * @instance
             * @memberof Collection
             * @param {string} collectionLink - resource link of the collection under which the document will be created
             * @param {Object} body - <p>body of the document<br />The "id" property is required and will be generated automatically if not provided (this behaviour can be overriden using the CreateOptions). Any other properties can be added.</p>
             * @param {Collection.CreateOptions} [options] - optional create options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the create has been queued, false if it is not queued because of a pending timeout.
             */
            this.createDocument = function (collectionLink, body, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'createDocument', 2, arguments.length));
                }
                if (body === null || !(typeof body === "object" || typeof body === "string")) {
                    throw new Error(ErrorCodes.BadRequest, errorMessages.docBodyMustBeObjectOrString);
                }

                var collectionIdPair = validateCollectionLink(collectionLink);
                var collectionResourceIdentifier = collectionIdPair.collId;
                var isNameRouted = collectionIdPair.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                // Generate random document id if the id is missing in the payload and options.disableAutomaticIdGeneration != true
                if (options.disableAutomaticIdGeneration !== true) {
                    var bodyObject = body;
                    if (typeof body === 'string') {
                        bodyObject = JSON.parse(body);
                    }

                    if (!bodyObject.id) { // check for undefined, null, "" etc
                        if (bodyObject === body) {
                            // Clone the body so user's object remains immutable
                            // We only need to shallow clone the object since we're adding the 'id' property to first level.
                            var bodyClone = {};
                            for (var propertyName in body) {
                                bodyClone[propertyName] = body[propertyName];
                            }
                            bodyObject = bodyClone;
                        }

                        bodyObject.id = generateGuidId();
                        body = bodyObject;
                    }
                }

                // stringify if either a) passed in as object b) passed in as string without id
                if (typeof body === 'object') {
                    body = JSON.stringify(body);
                }

                var indexAction = options.indexAction || '';
                return collectionObjRaw.create(resourceTypes.document, collectionResourceIdentifier, isNameRouted, body, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Upsert a document under the collection.
             * @name upsertDocument
             * @function
             * @instance
             * @memberof Collection
             * @param {string} collectionLink - resource link of the collection under which the document will be upserted
             * @param {Object} body - <p>body of the document<br />The "id" property is required and will be generated automatically if not provided (this behaviour can be overriden using the UpsertOptions). Any other properties can be added.</p>
             * @param {Collection.UpsertOptions} [options] - optional upsert options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the upsert has been queued, false if it is not queued because of a pending timeout.
             */
            this.upsertDocument = function (collectionLink, body, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'upsertDocument', 2, arguments.length));
                }
                if (body === null || !(typeof body === "object" || typeof body === "string")) {
                    throw new Error(ErrorCodes.BadRequest, errorMessages.docBodyMustBeObjectOrString);
                }

                var collectionIdPair = validateCollectionLink(collectionLink);
                var collectionResourceIdentifier = collectionIdPair.collId;
                var isNameRouted = collectionIdPair.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                // Generate random document id if the id is missing in the payload and options.disableAutomaticIdGeneration != true
                if (options.disableAutomaticIdGeneration !== true) {
                    var bodyObject = body;
                    if (typeof body === 'string') {
                        bodyObject = JSON.parse(body);
                    }

                    if (!bodyObject.id) { // check for undefined, null, "" etc
                        if (bodyObject === body) {
                            // Clone the body so user's object remains immutable
                            // We only need to shallow clone the object since we're adding the 'id' property to first level.
                            var bodyClone = {};
                            for (var propertyName in body) {
                                bodyClone[propertyName] = body[propertyName];
                            }
                            bodyObject = bodyClone;
                        }

                        bodyObject.id = generateGuidId();
                        body = bodyObject;
                    }
                }

                // stringify if either a) passed in as object b) passed in as string without id
                if (typeof body === 'object') {
                    body = JSON.stringify(body);
                }

                var indexAction = options.indexAction || '';
                var etag = options.etag || '';
                return collectionObjRaw.upsert(resourceTypes.document, collectionResourceIdentifier, isNameRouted, body, etag, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Replace a document.
             * @name replaceDocument
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document
             * @param {Object} document - new document body
             * @param {Colleciton.ReplaceOptions} [options] - optional replace options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the replace has been queued, false if it is not queued because of a pending timeout.
             */
            this.replaceDocument = function (documentLink, document, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'replaceDocument', 2, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, false);
                var collectionRid = documentIdTuple.collId;
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                if (typeof document === 'object') {
                    document = JSON.stringify(document);
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var indexAction = options.indexAction || '';
                var etag = options.etag || '';
                return collectionObjRaw.replace(resourceTypes.document, collectionRid, documentResourceIdentifier, isNameRouted, document, etag, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Delete a document.
             * @name deleteDocument
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document to delete
             * @param {Collection.DeleteOptions} [options] - optional delete options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the delete has been queued, false if it is not queued because of a pending timeout.
             */
            this.deleteDocument = function (documentLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'deleteDocument', 1, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, false);
                var collectionRid = documentIdTuple.collId;
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var etag = options.etag || '';
                return collectionObjRaw.deleteResource(
                    resourceTypes.document,
                    collectionRid,
                    documentResourceIdentifier,
                    isNameRouted,
                    etag,
                    options.partitionKeyContent,    // This must be JSON-serialized.
                    function (err, response) {
                        if (callback) {
                            if (err) callback(err);
                            else callback(undefined, response.options);
                        } else if (err) throw err;
                    });
            };
            //---------------------------------------------------------------------------------------------------
            // Attachment interface
            //---------------------------------------------------------------------------------------------------
            /**
             * Read an Attachment.
             * @name readAttachment
             * @function
             * @instance
             * @memberof Collection
             * @param {string} attachmentLink - resource link of the attachment to read
             * @param {Collection.ReadOptions} [options] - optional read options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the read has been queued, false if it is not queued because of a pending timeout.
             */
            this.readAttachment = function (attachmentLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'readAttachment', 1, arguments.length));
                }

                var attachmentIdTuple = validateAttachmentLink(attachmentLink);
                var documentRid = attachmentIdTuple.docId;
                var attachmentResourceIdentifier = attachmentIdTuple.attId;
                var isNameRouted = attachmentIdTuple.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var ifNoneMatch = options.ifNoneMatch || '';
                return collectionObjRaw.read(resourceTypes.attachment, documentRid, attachmentResourceIdentifier, isNameRouted, ifNoneMatch, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else if (response.options.notModified) {
                            callback(undefined, undefined, response.options);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Get all attachments for the document.
             * @name readAttachments
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document whose attachments are being read
             * @param {Collection.FeedOptions} [options] - optional read feed options
             * @param {Collection.FeedCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the read has been queued, false if it is not queued because of a pending timeout.
             */
            this.readAttachments = function (documentLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'readAttachments', 1, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, true);
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var pageSize = options.pageSize || 100;
                var requestContinuation = options.continuation || '';
                return collectionObjRaw.readFeed(resourceTypes.attachment, documentResourceIdentifier, isNameRouted, requestContinuation, pageSize, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body).Attachments, response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Execute a SQL query on the attachments for the document.
             * @name queryAttachments
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document whose attachments are being queried
             * @param {string} query - SQL query string. This can also be a JSON object to pass in a parameterized query along with the values.
             * @param {Collection.FeedOptions} [options] - optional query options
             * @param {Collection.FeedCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the query has been queued, false if it is not queued because of a pending timeout.
             */
            this.queryAttachments = function (documentLink, filterQuery, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'queryAttachments', 2, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, true);
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                if (typeof filterQuery !== 'string' && typeof filterQuery !== 'object') {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidParamType, 'filterQuery', '"string" or "object"', typeof filterQuery));
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var pageSize = options.pageSize || 100;
                var requestContinuation = options.continuation || '';
                var enableScan = options.enableScan === true;
                return collectionObjRaw.query(resourceTypes.attachment, documentResourceIdentifier, isNameRouted, filterQuery, requestContinuation, pageSize, enableScan, false, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body).Attachments, response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /** Create an attachment for the document.
             * @name createAttachment
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document under which the attachment will be created
             * @param {Object} body - <p>metadata that defines the attachment media like media, contentType<br />It can include any other properties as part of the metedata.</p>
             * @param {string} body.contentType - MIME contentType of the attachment
             * @param {string} body.media - media link associated with the attachment content
             * @param {Collection.CreateOptions} [options] - optional create options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the create has been queued, false if it is not queued because of a pending timeout.
             */
            this.createAttachment = function (documentLink, body, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'createAttachment', 2, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, false);
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                if (typeof body === 'object') {
                    body = JSON.stringify(body);
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var indexAction = options.indexAction || '';
                return collectionObjRaw.create(resourceTypes.attachment, documentResourceIdentifier, isNameRouted, body, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /** Upsert an attachment for the document.
             * @name upsertAttachment
             * @function
             * @instance
             * @memberof Collection
             * @param {string} documentLink - resource link of the document under which the attachment will be upserted
             * @param {Object} body - <p>metadata that defines the attachment media like media, contentType<br />It can include any other properties as part of the metedata.</p>
             * @param {string} body.contentType - MIME contentType of the attachment
             * @param {string} body.media - media link associated with the attachment content
             * @param {Collection.UpsertOptions} [options] - optional upsert options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the upsert has been queued, false if it is not queued because of a pending timeout.
             */
            this.upsertAttachment = function (documentLink, body, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'upsertAttachment', 2, arguments.length));
                }

                var documentIdTuple = validateDocumentLink(documentLink, false);
                var documentResourceIdentifier = documentIdTuple.docId;
                var isNameRouted = documentIdTuple.isNameRouted;

                if (typeof body === 'object') {
                    body = JSON.stringify(body);
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var indexAction = options.indexAction || '';
                var etag = options.etag || '';
                return collectionObjRaw.upsert(resourceTypes.attachment, documentResourceIdentifier, isNameRouted, body, etag, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Replace an attachment.
             * @name replaceAttachment
             * @function
             * @instance
             * @memberof Collection
             * @param {string} attachmentLink - resource link of the attachment to be replaced
             * @param {Object} attachment - new attachment body
             * @param {Colleciton.ReplaceOptions} [options] - optional replace options
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the replace has been queued, false if it is not queued because of a pending timeout.
             */
            this.replaceAttachment = function (attachmentLink, attachment, options, callback) {
                if (arguments.length < 2) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'replaceAttachment', 2, arguments.length));
                }

                var attachmentIdTuple = validateAttachmentLink(attachmentLink);
                var documentRid = attachmentIdTuple.docId;
                var attachmentResourceIdentifier = attachmentIdTuple.attId;
                var isNameRouted = attachmentIdTuple.isNameRouted;

                if (typeof attachment === 'object') {
                    attachment = JSON.stringify(attachment);
                }

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var indexAction = options.indexAction || '';
                var etag = options.etag || '';
                return collectionObjRaw.replace(resourceTypes.attachment, documentRid, attachmentResourceIdentifier, isNameRouted, attachment, etag, indexAction, function (err, response) {
                    if (callback) {
                        if (err) {
                            callback(err);
                        } else {
                            callback(undefined, JSON.parse(response.body), response.options);
                        }
                    } else {
                        if (err) {
                            throw err;
                        }
                    }
                });
            };
            //---------------------------------------------------------------------------------------------------
            /**
             * Delete an attachment.
             * @name deleteAttachment
             * @function
             * @instance
             * @memberof Collection
             * @param {string} attachmentLink - resource link of the attachment to be deleted
             * @param {Collection.DeleteOptions} [options] - optional delete options.
             * @param {Collection.RequestCallback} [callback] - <p>optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown.</p>
             * @return {Boolean} True if the delete has been queued, false if it is not queued because of a pending timeout.
             */
            this.deleteAttachment = function (attachmentLink, options, callback) {
                if (arguments.length === 0) {
                    throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'deleteAttachment', 1, arguments.length));
                }

                var attachmentIdTuple = validateAttachmentLink(attachmentLink);
                var documentRid = attachmentIdTuple.docId;
                var attachmentResourceIdentifier = attachmentIdTuple.attId;
                var isNameRouted = attachmentIdTuple.isNameRouted;

                var optionsCallbackTuple = validateOptionsAndCallback(options, callback);
                options = optionsCallbackTuple.options;
                callback = optionsCallbackTuple.callback;

                var etag = options.etag || '';
                return collectionObjRaw.deleteResource(
                    resourceTypes.attachment,
                    documentRid,
                    attachmentResourceIdentifier,
                    isNameRouted,
                    etag,
                    undefined,   // pkContent
                    function (err, response) {
                        if (callback) {
                            if (err) callback(err);
                            else callback(undefined, response.options);
                        } else if (err) throw err;
                    });
            };
            //---------------------------------------------------------------------------------------------------
            // Returns PK definition in the format like [ [ "name", "first" ] ].
            // Internal.
            Object.defineProperty(
                this,
                "getPartitionKeyDefinition",
                { value: function () { return collectionObjRaw.getPartitionKeyDefinition(); } });
        } // DocDbCollection.

        // Set up JS Query/underscore-like API functions.
        var setupJSQuery;
        if (typeof __docDbJSQueryEnabled !== "undefined") {
            function DocDb__() {
                var queryFunctionImpls = {
                    "filter": filterImpl,
                    "map": mapImpl,
                    "sortBy": passThroughImpl,
                    "sortByDescending": passThroughImpl,
                    "flatten": flattenImpl,
                    "unwind": unwindImpl,
                };

                var capturesPropertyName = "captures";

                function filterImpl(feed, predicateFn) {
                    // Note: assuming the feed is Array.
                    return Array.prototype.filter.call(feed, predicateFn);
                }

                function mapImpl(feed, transformFn) {
                    // Note: assuming the feed is Array.
                    return Array.prototype.map.call(feed, transformFn);
                }

                function passThroughImpl(feed) {
                    return feed;
                }

                function flattenImpl(isShallow, feed) {
                    var result = new Array();
                    if (isShallow === true) {
                        // Note: assuming the feed is Array. Take advantage of Array.prototype.concat which flattens 1 level.
                        feed.forEach(function (x) { result = result.concat(x); });
                    } else {
                        return feed;
                    }
                    return result;
                }

                function unwindImpl(feed, collectionAndResultSelectors) {
                    var collectionSelector, resultSelector;
                    if (collectionAndResultSelectors) {
                        collectionSelector = collectionAndResultSelectors[0];
                        resultSelector = collectionAndResultSelectors[1];
                    }

                    var result = new Array();

                    if (collectionSelector) {
                        feed.forEach(function (x) {
                            var collection = collectionSelector(x);
                            if (resultSelector) {
                                collection.forEach(function (y) {
                                    var temp = resultSelector(x, y);
                                    result = result.concat(temp);
                                });
                            } else {
                                result = result.concat(collection);
                            }
                        });
                    }

                    return result;
                }

                function defaultUserCallback(err, feed, options) {
                    // If user callback is not specified, results go response body (as append) and options are ignored. Simple usage.
                    if (err) throw err;
                    if (!getContext().getResponse) throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_missingCallbackWhenNoResponse);

                    getContext().getResponse().appendBody(JSON.stringify(feed));
                }

                function canonicalizeOptions(options, queryObject, userCallback) {
                    var optionsCallbackTuple = validateOptionsAndCallback(options, userCallback);
                    options = optionsCallbackTuple.options;
                    userCallback = optionsCallbackTuple.callback ? optionsCallbackTuple.callback : defaultUserCallback;

                    return {
                        pageSize: options.pageSize || 100,
                        continuation: options.continuation || "",
                        enableScan: options.enableScan === true,
                        enableLowPrecisionOrderBy: options.enableLowPrecisionOrderBy === true,
                        onComplete: completionCallback.bind(this, queryObject, userCallback)
                    };
                }

                function completionCallback(queryObject, userCallback, err, response) {
                    function handleError(err) {
                        if (userCallback) userCallback(err);
                        else throw err;
                    } // returns undefined by design.

                    if (err) return handleError(err);

                    var result = JSON.parse(response.body).Documents;

                    try {
                        // Call each predicate/transform callback in the chain passing output from prev as input to next.
                        queryObject.callbacks.forEach(function (current) {
                            result = current.fn(result, current.callback);
                        });
                    } catch (ex) {
                        return handleError(ex);
                    }

                    if (userCallback) userCallback(undefined, result, response.options);
                }

                function defineJSQueryFunctions(objectToAddTo, queryObject) {
                    if (queryObject && queryObject.isAccepted === false) {
                        var propagate = function () { return this };
                        objectToAddTo.filter = propagate;
                        objectToAddTo.map = propagate;
                        objectToAddTo.sortBy = propagate;
                        objectToAddTo.sortByDescending = propagate;
                        objectToAddTo.flatten = propagate;
                        objectToAddTo.pluck = propagate;
                        objectToAddTo.unwind = propagate;
                        objectToAddTo.value = propagate;
                    } else {
                        // .filter/map([collection], predicate, [options], [callback])
                        objectToAddTo.filter = function (collectionObject, predicateCallback, options, userCallback) {
                            return javaScriptQuery.call(this, queryObject, "filter", collectionObject, predicateCallback, options, userCallback);
                        };
                        objectToAddTo.map = function (collectionObject, predicateCallback, options, userCallback) {
                            return javaScriptQuery.call(this, queryObject, "map", collectionObject, predicateCallback, options, userCallback);
                        };

                        // sortBy/sortByDescending([collection], iteratee, [options], [callback])
                        objectToAddTo.sortBy = function (collectionObject, iterateeCallback, options, userCallback) {
                            return javaScriptQuery.call(this, queryObject, "sortBy", collectionObject, iterateeCallback, options, userCallback);
                        };
                        objectToAddTo.sortByDescending = function (collectionObject, iterateeCallback, options, userCallback) {
                            return javaScriptQuery.call(this, queryObject, "sortByDescending", collectionObject, iterateeCallback, options, userCallback);
                        };

                        // flatten([collection], [isShallow], [options], [callback])
                        objectToAddTo.flatten = function (collectionObject, isShallow, options, userCallback) {
                            return flatten.call(this, queryObject, collectionObject, isShallow, options, userCallback);
                        };

                        // unwind([collection], [collectionSelector], [resultsSelector], [options], [callback])
                        objectToAddTo.unwind = function (collectionObject, collectionSelector, resultSelector, options, userCallback) {
                            return unwind.call(this, queryObject, collectionObject, collectionSelector, resultSelector, options, userCallback);
                        };

                        // pluck([collection], propertyName, [options], [callback])
                        objectToAddTo.pluck = function (collectionObject, propertyName, options, callback) {
                            return pluck.call(this, queryObject, collectionObject, propertyName, options, callback)
                        };

                        if (this) {
                            objectToAddTo[capturesPropertyName] = this[capturesPropertyName];
                        }
                    }
                }

                function continueJavaScriptQuery(prevQueryObject, options, userCallback, queryFunctionChained, queryFunctionPlain, callbackObject) {
                    if (prevQueryObject) {
                        // We are in chained call.
                        var queryObject = queryFunctionChained.call(this);

                        // Add properties on the query object for further chaining.
                        defineJSQueryFunctions.call(this, queryObject, queryObject);

                        if (!queryObject.isAccepted) return queryObject;

                        queryObject.callbacks.push(callbackObject);

                        queryObject.value = function (options, valueUserCallback) { // .value([options], [callback])
                            if (typeof options === "function") {
                                valueUserCallback = options;
                                options = undefined;
                            }
                            options = canonicalizeOptions.call(this, options, queryObject, valueUserCallback);
                            return collectionObjRaw.value(this, options.continuation, options.pageSize, options.enableScan, options.enableLowPrecisionOrderBy, options.onComplete);
                        }

                        return queryObject;
                        // TODO: Move callbacks to native query object so that they are not visible to the user.
                    } else {
                        // We are not in chained call. The completion callback will be called.
                        // Cook up queryObject for non-chained call.
                        var queryObject = { callbacks: [callbackObject] };
                        options = canonicalizeOptions.call(this, options, queryObject, userCallback);
                        return queryFunctionPlain.call(this, options);
                    }
                }

                function javaScriptQuery(prevQueryObject, queryFunctionName, collectionObject, predicateCallback, options, userCallback, actualFunctionName) {
                    if (typeof collectionObject === "undefined") {
                        collectionObject = getContext().getCollection();
                    } else if (typeof collectionObject === "function") {
                        userCallback = options;
                        options = predicateCallback;
                        predicateCallback = collectionObject;
                        collectionObject = getContext().getCollection();
                    }

                    if (typeof predicateCallback !== "function") {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_missingPredicate);
                    }

                    if (typeof options === "function") {
                        userCallback = options;
                        options = undefined;
                    }

                    if (prevQueryObject && options) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_optionsInvalidInChain);
                    }

                    if (prevQueryObject && userCallback) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_callbackInvalidInChain);
                    }

                    if (!actualFunctionName) actualFunctionName = queryFunctionName;
                    return continueJavaScriptQuery.call(
                        this,
                        prevQueryObject,
                        options,
                        userCallback,
                        function () {   // chained case.
                            return collectionObjRaw[queryFunctionName](
                                this[capturesPropertyName], prevQueryObject, predicateCallback, "", 0, false, false, undefined, actualFunctionName);
                        },
                        function (options) {    // non-chained case.
                            return collectionObjRaw[queryFunctionName](
                                this[capturesPropertyName], undefined, predicateCallback, options.continuation, options.pageSize, options.enableScan, options.enableLowPrecisionOrderBy, options.onComplete, actualFunctionName);
                        },
                        { fn: queryFunctionImpls[queryFunctionName], callback: predicateCallback }
                    );
                }

                function flatten(prevQueryObject, collectionObject, isShallow, options, userCallback) {
                    if (typeof collectionObject === "undefined") {
                        // We can get here when either collection is explicitly passed undefined or all args are missing.
                        collectionObject = getContext().getCollection();
                    } else if (collectionObject !== docDbCollection) {
                        userCallback = options;
                        options = isShallow;
                        isShallow = collectionObject;
                        collectionObject = getContext().getCollection();
                    }

                    if (typeof isShallow !== "boolean") {
                        userCallback = options;
                        options = isShallow;
                        isShallow = false;
                    }

                    if (typeof options != "object") {
                        userCallback = options;
                        options = undefined;
                    }

                    if (prevQueryObject && options) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_optionsInvalidInChain);
                    }

                    if (prevQueryObject && userCallback) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_callbackInvalidInChain);
                    }

                    return continueJavaScriptQuery.call(
                        this,
                        prevQueryObject,
                        options,
                        userCallback,
                        function () {
                            return collectionObjRaw.flatten(
                                this[capturesPropertyName], prevQueryObject, "", 0, undefined);
                        },
                        function (options) {
                            return collectionObjRaw.flatten(
                                this[capturesPropertyName], undefined, options.continuation, options.pageSize, options.onComplete);
                        },
                        { fn: flattenImpl.bind(this, isShallow) }
                    );
                }

                // collection/result selector win over userCallback
                //   In order to use collectionSelector and userCallback (without resultSelector), options must be provided, as
                //   typically, userCallback and options would be used out of value(), so they normally should not be part of unwind.
                function unwind(prevQueryObject, collectionObject, collectionSelector, resultSelector, options, userCallback) {
                    if (typeof collectionObject === "undefined") {
                        // We can get here when either collection is explicitly passed undefined or all args are missing.
                        collectionObject = getContext().getCollection();
                    } else if (collectionObject !== docDbCollection) {
                        userCallback = options
                        options = resultSelector;
                        resultSelector = collectionSelector;
                        collectionSelector = collectionObject;
                        collectionObject = getContext().getCollection();
                    }

                    if (!collectionSelector) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_collectionSelectorIsRequired);
                    } else if (typeof collectionSelector != "function") {
                        // Note: currently the collectionSelector has to be a function. We may later add support for queryObject.
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_collectionSelectorMustBeFunction);
                    } else if (collectionSelector.length != 1) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_invalidCollectionSelectorArguments);
                    }

                    if (typeof resultSelector != "function") {
                        userCallback = options;
                        options = resultSelector;
                        resultSelector = undefined;
                    } else if (resultSelector.length < 2) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_invalidResultSelectorArguments);
                    }

                    if (typeof options != "object") {
                        userCallback = options;
                        options = undefined;
                    }

                    if (!collectionSelector && resultSelector) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_isResultSelectorRequiresCollectionSelector);
                    }

                    if (prevQueryObject && options) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_optionsInvalidInChain);
                    }

                    if (prevQueryObject && userCallback) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_callbackInvalidInChain);
                    }

                    return continueJavaScriptQuery.call(
                        this,
                        prevQueryObject,
                        options,
                        userCallback,
                        function () {
                            return collectionObjRaw.flatten(
                                this[capturesPropertyName], prevQueryObject, collectionSelector, resultSelector, "", 0, undefined);
                        },
                        function (options) {
                            return collectionObjRaw.flatten(
                                this[capturesPropertyName], undefined, collectionSelector, resultSelector, options.continuation, options.pageSize, options.onComplete);
                        },
                        { fn: unwindImpl.bind(this), callback: [collectionSelector, resultSelector] }
                    );
                }

                function pluck(prevQueryObject, collectionObject, propertyName, options, callback) {
                    // Take care of propertyName, all the rest is taken care of by javaScriptQuery.
                    if (typeof collectionObject === "string") {
                        callback = options;
                        options = propertyName;
                        propertyName = collectionObject;
                        collectionObject = undefined;
                    }

                    if (prevQueryObject && options) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_optionsInvalidInChain);
                    }

                    if (prevQueryObject && callback) {
                        throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_callbackInvalidInChain);
                    }

                    if (typeof propertyName != "string" || !propertyName) throw new Error(ErrorCodes.BadRequest, errorMessages.jsQuery_wrongPropertyName);

                    // Make sure we have propertyName in captures so that x[propertyName] gets optimized.
                    var newThis = this;
                    if (!newThis[capturesPropertyName]) {
                        newThis = proxy.call(this, { propertyName: propertyName });
                    } else if (!newThis[capturesPropertyName]["propertyName"]) {
                        newThis[capturesPropertyName]["propertyName"] = propertyName;
                    }
                    // else if this property is already captured, it will have same value => nothing to do.

                    // Delegate to map.
                    return javaScriptQuery.call(newThis, prevQueryObject, "map", collectionObject, function (x) { return x[propertyName]; }, options, callback, "pluck");
                }

                function proxy(captures) {
                    function Proxy() { }
                    Proxy.prototype = this;
                    var p = new Proxy();
                    p[capturesPropertyName] = captures; // Note: this must be writable as we may add more captured vars along the chain (see pluck).
                    return p;
                }

                function setupSpatial() {
                    this.distance = function (geoJSON1, geoJSON2) {
                        if (arguments.length < 2) {
                            throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'distance', 2, arguments.length));
                        }

                        if (typeof geoJSON1 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON1"));
                        if (typeof geoJSON2 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON2"));

                        geoJSON1 = JSON.stringify(geoJSON1);
                        geoJSON2 = JSON.stringify(geoJSON2);

                        return collectionObjRaw.SpatialDistance(geoJSON1, geoJSON2);
                    }

                    this.intersects = function (geoJSON1, geoJSON2) {
                        if (arguments.length < 2) {
                            throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'intersects', 2, arguments.length));
                        }

                        if (typeof geoJSON1 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON1"));
                        if (typeof geoJSON2 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON2"));

                        geoJSON1 = JSON.stringify(geoJSON1);
                        geoJSON2 = JSON.stringify(geoJSON2);

                        return collectionObjRaw.SpatialIntersects(geoJSON1, geoJSON2);
                    }

                    this.isValid = function (geoJSON) {
                        if (arguments.length < 1) {
                            throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'isValid', 1, arguments.length));
                        }

                        if (typeof geoJSON !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON"));

                        geoJSON = JSON.stringify(geoJSON);

                        return collectionObjRaw.SpatialIsValid(geoJSON);
                    }

                    this.isValidDetailed = function (geoJSON) {
                        if (arguments.length < 1) {
                            throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'isValidDetailed', 1, arguments.length));
                        }

                        if (typeof geoJSON !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON"));

                        geoJSON = JSON.stringify(geoJSON);

                        return collectionObjRaw.SpatialIsValidDetailed(geoJSON);
                    }

                    this.within = function (geoJSON1, geoJSON2) {
                        if (arguments.length < 2) {
                            throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'within', 2, arguments.length));
                        }

                        if (typeof geoJSON1 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON1"));
                        if (typeof geoJSON2 !== "object") throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidGeoJSON, "geoJSON2"));

                        geoJSON1 = JSON.stringify(geoJSON1);
                        geoJSON2 = JSON.stringify(geoJSON2);

                        return collectionObjRaw.SpatialWithin(geoJSON1, geoJSON2);
                    }
                }

                defineJSQueryFunctions.call(this, this, undefined);

                this.chain = function (collectionObject) {                      // __.chain([collection])
                    var queryObject = collectionObjRaw.chain(getContext().getCollection());
                    if (queryObject.isAccepted) {
                        queryObject.callbacks = new Array();
                    }

                    defineJSQueryFunctions.call(this, queryObject, queryObject);
                    return queryObject;
                }

                Object.defineProperty(this, "proxy", { // Used to pass captures.
                    value: proxy // keep enumerable/configurable/writable as false by default.
                });

                this.spatial = new Object();
                setupSpatial.call(this.spatial);
            } // __.

            setupJSQuery = DocDb__;
            __docDbJSQueryEnabled = undefined;
        } // if __docDbJSQueryEnabled.

        function initializeCollectionObject() {
            if (typeof __docDbCollectionObjectRaw !== "undefined") {
                collectionObjRaw = __docDbCollectionObjectRaw;
                __docDbCollectionObjectRaw = undefined;
            }
        }

        initializeCollectionObject();

        // This object contains all the CosmosDB JavaScript APIs. This object is not exposed directly instead it is 
        // returned when getContext().getCollection() is called and is also added as __proto__ object to __
        var docDbCollection = new DocDbCollection();

        if (setupJSQuery) {
            setupJSQuery.call(docDbCollection);
        }

        // Create __ object with docDbCollection as the prototype
        __ = Object.create(docDbCollection);

        getContext = function () { return contextObj; };

        // Purely virtual but we need to re-create it every time, as req/resp may be missing.
        function DocDbContext() {
            this.getCollection = function () { return docDbCollection; }
            this.abort = function (err) { collectionObjRaw.abort(err); }
        }

        // This method initializes context, request and response objects. We create a function object as this function will be used to
        // re-initialize these objects in scenarios where the same session is used multiple times. This is achieved by returning this 
        // function object to the caller of the script to use during re-initialize. Refer __docDbReinitializableSession object and 
        // DocDbWrapperScriptReinitialize.js for more details.
        function setupContextObjects() {
            if (typeof __docDbCollectionObjectRaw !== "undefined") {
                initializeCollectionObject();
            }
            contextObj = new DocDbContext();
            docDbSetupRequestResponseObjects();

            // Create Request and Response properties in __ 
            if (contextObj.getRequest) {
                __.request = contextObj.getRequest();
            } else if (__.request) {
                delete __.request;
            }

            if (contextObj.getResponse) {
                __.response = contextObj.getResponse();
            } else if (__.response) {
                delete __.response;
            }

            // Setup console log method
            if (__.request && __.request["get" + scriptLoggingRequestHeaderName] && __.request.getValue(scriptLoggingRequestHeaderName) === "true") {
                console[consoleMethodNames.log] = function (logArgs) {
                    if (arguments.length < 1) {
                        throw new Error(ErrorCodes.BadRequest, sprintf(errorMessages.invalidFunctionCall, 'log', 1, arguments.length));
                    } else if (arguments.length === 1) {
                        if (contextObj.getResponse) {
                            contextObj.getResponse().appendValue(scriptLoggingResponseHeaderName, logArgs);
                        }
                    } else {
                        if (typeof arguments[0] == "string") {
                            if (contextObj.getResponse) {
                                contextObj.getResponse().appendValue(scriptLoggingResponseHeaderName, sprintf.apply(this, arguments));
                            }
                        } else {
                            throw new Error(ErrorCodes.BadRequest, errorMessages.invalidVariableSubstitution);
                        }
                    }
                }
            } else {
                console[consoleMethodNames.log] = function () { };
            }

            // cleanup global objects
            __docDbRequestProperties = undefined;
            __docDbResponseProperties = undefined;
        }

        setupContextObjects();

        // If the wrapper script is run in a re-initializable session (system sproc session), then we need to return the setupContextObjects function to the caller.
        if (typeof __docDbReinitializableSession !== "undefined") {
            if (__docDbReinitializableSession) {
                __docDbReinitializeContextFunc = setupContextObjects;
            }
            __docDbReinitializableSession = undefined;
        }
    })(); // docDbSetupLocalStoreOperations.

})(); // docDbSetup.
//---------------------------------------------------------------------------------------------------
//------------------------------------------------- Documentation Types -----------------------------

/**
 * <p>Holds the <a href="-__object.html">__</a> object.</p>
 *   @name __
 *   @type {__object}
 *   @global
 */

/** <p>The __ object can be used as a shortcut to the <a href="Collection.html">Collection</a> and <a href="Context.html">Context</a> objects.
 *  It derives from the <a href="Collection.html">Collection</a> object via prototype and defines request and response properties
 *  which are shortcuts to <a href="Context.html#getRequest">getContext().getRequest()</a> and <a href="Context.html#getResponse">getContext().getResponse()</a>.</p>
 *   @name __object
 *   @class
 *   @implements {Collection}
 *   @property {Request}    request     Alias for getContext().getRequest()
 *   @property {Response}   response    Alias for getContext().getResponse()
 *   @example var result = __.filter(function(doc) { return doc.id == 1; });
 if(!result.isAccepted) throw new Error("the call was not accepted");
 */

/**
 * <p>Execute a filter on the input stream of documents, resulting in a subset of the input stream that matches the given filter.<br/>
 * When filter is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name filter
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.FilterPredicate} predicate - Predicate function defining the filter
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @example // Example 1: get documents(people) with age < 30.
 var result = __.filter(function(doc) { return doc.age < 30; });
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 2: get documents (people) with age < 30 and select only name.
 var result = __.chain()
 .filter(function(doc) { return doc.age < 30; })
 .pluck("name")
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 3: get document (person) with id = 1 and delete it.
 var result = __.filter(function(doc) { return doc.id === 1; }, function(err, feed, options) {
    if(err) throw err;
    if(!__.deleteDocument(feed[0].getSelfLink())) throw new Error("deleteDocument was not accepted");
});
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Produce a new set of documents by mapping/projecting the properties of the documents in the input document stream through the given mapping predicate.<br/>
 * When map is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name map
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.ProjectionPredicate} predicate - Predicate function defining the projection
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @example // Example 1: select only name and age for each document (person).
 var result = __.map(function(doc){ return { name: doc.name, age: doc.age}; });
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 2: select name and age for each document (person), and return only people with age < 30.
 var result = __.chain()
 .map(function(doc) { return { name: doc.name, age: doc.age}; })
 .filter(function(doc) { return doc.age < 30; })
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Produce a new set of documents by extracting a single property from each document in the input document stream. This is equivalent to a <a href="#map">map</a> call that projects only propertyName.<br/>
 * When pluck is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name pluck
 * @function
 * @instance
 * @memberof Collection
 * @param {string} propertyName - Name of the property to pluck from all documents in the current collection
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @example // Get documents (people) with age < 30 and select only name.
 var result = __.chain()
 .filter(function(doc) { return doc.age < 30; })
 .pluck("name")
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Flatten nested arrays from each document in the input document stream.<br/>
 * When flatten is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name flatten
 * @function
 * @instance
 * @memberof Collection
 * @param {Boolean} [isShallow] - If true, flattens only the first level of nested arrays (false by default)
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @example // Get documents (people) with age < 30, select tags (an array property)
 // and flatten the result into one array for all documents.
 var result = __.chain()
 .filter(function(doc) { return doc.age < 30; })
 .map(function(doc) { return doc.tags; })
 .flatten()
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Perfoms a join with inner collection with both top level document item and inner collection item added to the result projection.<br/>
 * When resultSelector is provided, resultSelector is called for each pair of &lt;current document, inner collection item&gt;.</p>
 * When resultSelector is not provided, __.unwind() just adds inner collection to the result projection. In this case unwind() is equivalent to map(innerCollectionSelector).flatten().
 * Calls to unwind can be chained to perform multiple joins.
 * @name unwind
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.ProjectionPredicate} innerCollectionSelector - Predicate function defining the projection for inner collection.
 * @param {Collection.ResultSelectorPredicate} [resultSelector] - Optional predicate function defining result projection.
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @example
 // Get <customer, kids, pet> tuples from customer collection:
 //
 var result = __.chain()
 .unwind(c => c.kids, (c, k) => { return { customer: c, kid: k } })
 .unwind(ck => ck.kid.pets, (ck, p) => { return { customer: ck.customer.name, kid: ck.kid.name, pet: p.name }})
 .value();
 if(!result.isAccepted) throw new Error("one of the calls was not accepted");
 //
 // With the following input data:
 // [{
 //         "id": "1",
 //         "name": "Alex",
 //         "kids": [{
 //                 "name": "Bob",
 //                 "pets": [
 //                     { "name": "Chucky" },
 //                     { "name": "Chauncey" },
 //                 ]
 //             },
 //             {
//                 "name": "Bill",
//                 "pets": [
//                     { "name": "Charcoral" },
//                     { "name": "Cookie" }
//                 ]
//             }
 //         ]
 //     },
 //     {
//         "id": "2",
//         "name": "Alice",
//         "kids": [{
//                 "name": "Barbara",
//                 "pets": [
//                     { "name": "Copper" },
//                     { "name": "Curly" }
//                 ]
//             },
//             {
//                 "name": "Beth",
//                 "pets": [
//                     { "name": "Cutie" }
//                 ]
//             }
//         ]
//     }
 // ]
 //
 // The result would be:
 //
 // [
 //     { "customer": "Alex", "kid": "Bob", "pet": "Chucky" },
 //     { "customer": "Alex", "kid": "Bob", "pet": "Chauncey" },
 //     { "customer": "Alex", "kid": "Bill", "pet": "Charcoral" },
 //     { "customer": "Alex", "kid": "Bill", "pet": "Cookie" },
 //     { "customer": "Alice", "kid": "Barbara", "pet": "Copper" },
 //     { "customer": "Alice", "kid": "Barbara", "pet": "Curly" },
 //     { "customer": "Alice", "kid": "Beth", "pet": "Cutie" }
 // ]
 //
 // This is equivalent to:
 // SELECT
 //   c.name as customer,
 //   k.name as kid,
 //   p.name as pet
 // FROM customer c
 // JOIN k in c.kids
 // JOIN p in k.pets
 */

/**
 * <p>Produce a new set of documents by sorting the documents in the input document stream in ascending order using the given predicate.<br/>
 * When sortBy is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name sortBy
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.SortByPredicate} predicate - Predicate function defining the property to sort by.
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @see <a href="#sortByDescending">sortByDescending</a> to sort in descending order.
 * @example // Example 1: sort documents (people) by age
 var result = __.sortBy(function(doc){ return doc.age; })
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 2: sortBy in a chain by name
 var result = __.chain()
 .filter(function(doc) { return doc.age < 30; })
 .sortBy(function(doc) { return doc.name; })
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Produce a new set of documents by sorting the documents in the input document stream in descending order using the given predicate.<br/>
 * When sortByDescending is called by itself, the input document stream is the set of all documents in the current document collection.
 * When used in a <a href="#chain">chained call</a>, the input document stream is the set of documents returned from the previous query function.</p>
 * @name sortByDescending
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.SortByPredicate} predicate - Predicate function defining the property to sort by.
 * @param {Collection.FeedOptions} [options] - Optional query options. Should not be used in a <a href="#chain">chained call</a>.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation.<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.<br/>Should not be used in a <a href="#chain">chained call</a>.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @see <a href="#sortBy">sortBy</a> to sort in ascending order.
 * @example // Example 1: sort documents (people) by age in descending order
 var result = __.sortByDescending(function(doc) { return doc.age; })
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 2: sortBy in a chain by name in descending order
 var result = __.chain()
 .filter(function(doc) { return doc.age < 30; })
 .sortByDescending(function(doc) { return doc.name; })
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * Opening call to start a chained query. Should be used in conjunction with the closing <a href="#value">value</a> call to perform chained queries.
 * @name chain
 * @function
 * @instance
 * @memberof Collection
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted. Can be used in a <a href="#chain">chained call</a> to call further queries.
 * @see The <a href="#value">value</a> call.
 * @example var name = "John";
 var result = __.chain()
 .filter(function(doc) { return doc.name == name; })
 .map(function(doc) { return { name: doc.name, age: doc.age }; })
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");
 */

/**
 * <p>Terminating call for a chained query. Should be used in conjunction with the opening <a href="#chain">chain</a> call to perform chained queries.<br/>
 * When value is called, the query is queued for execution with the given options and callback.</p>
 * @name value
 * @function
 * @instance
 * @memberof Collection
 * @param {Collection.FeedOptions} [options] - Optional query options for the entire chained call.
 * @param {Collection.FeedCallback} [callback] - <p>Optional callback for the operation<br/>If no callback is provided, any error in the operation will be thrown<br/>and the result document set will be written to the <a href="Response.html">Response</a> body.</p>
 * @return {Collection.QueryResponse} - Response which contains whether or not the query was accepted.
 * @see The <a href="#chain">chain</a> call.
 * @example // Example 1: use defaults: the result goes to the response body.
 __.chain()
 .filter(function(doc) { return doc.name == "John"; })
 .pluck("age"))
 .value();
 if(!result.isAccepted) throw new Error("The call was not accepted");

 // Example 2: use options and callback.
 function(continuationToken) {
    var result = __.chain()
        .filter(function(doc) { return doc.name == "John"; })
        .pluck("age"))
        .value({continuation: continuationToken}, function(err, feed, options) {
            if(err) throw err;
            __response.setBody({
                result: feed,
                continuation: options.continuation
            });
        });
    if(!result.isAccepted) throw new Error("The call was not accepted");
}
 */

/**
 * Options associated with a read operation.
 * @typedef {Object} ReadOptions                             -         Options associated with a read operation.
 * @property {string} [ifNoneMatch]                          -         The conditional HTTP method ifNoneMatch value.
 * @memberof Collection
 *
 */

/**
 * Options associated with a create operation.
 * @typedef {Object} CreateOptions                           -         Options associated with a create operation.
 * @property {string} [indexAction]                          -         Specifies indexing directives.
 * @property {string} indexAction.default                    -         use the default indexing policy specified for this collection
 * @property {string} indexAction.include                    -         include this document in the index
 * @property {string} indexAction.exclude                    -         exclude this document from the index
 * @property {string} [disableAutomaticIdGeneration]         -         Disables automatic generation of "id" field of the document to be created (if it is not provided)
 * @memberof Collection
 *
 */

/**
 * Options associated with a upsert operation.
 * @typedef {Object} UpsertOptions                           -         Options associated with a upsert operation.
 * @property {string} [indexAction]                          -         Specifies indexing directives.
 * @property {string} indexAction.default                    -         use the default indexing policy specified for this collection
 * @property {string} indexAction.include                    -         include this document in the index
 * @property {string} indexAction.exclude                    -         exclude this document from the index
 * @property {string} [disableAutomaticIdGeneration]         -         Disables automatic generation of "id" field of the document to be upserted (if it is not provided)
 * @memberof Collection
 *
 */

/**
 * Options associated with a replace operation.
 * @typedef {Object} ReplaceOptions                          -         Options associated with a replace operation.
 * @property {string} [indexAction]                          -         Specifies indexing directives.
 * @property {string} indexAction.default                    -         use the default indexing policy specified for this collection
 * @property {string} indexAction.include                    -         include this document in the index
 * @property {string} indexAction.exclude                    -         exclude this document from the index
 * @property {string} [etag]                                 -         <p>The entity tag associated with the resource.<br/>This is matched with the persisted resource before deletion.</p>
 * @memberof Collection
 *
 */

/**
 * Options associated with a delete operation.
 * @typedef {Object} DeleteOptions                           -         Options associated with a delete operation.
 * @property {string} [etag]                                 -         <p>The entity tag associated with the resource.<br/>This is matched with the persisted resource before deletion.</p>
 * @memberof Collection
 *
 */

/**
 * Options associated with a read feed or query operation.
 * @typedef {Object} FeedOptions                             -         Options associated with a read feed or query operation.
 * @property {Number} [pageSize]                             -         <p>Max number of items to be returned in the enumeration operation.<br/>Value is 100 by default</p>
 * @property {string} [continuation]                         -         Opaque token for continuing the enumeration.
 * @property {Boolean} [enableScan]                          -         Allow scan on the queries which couldn't be served as indexing was opted out on the requested paths (only for <a href="#queryDocuments">queryDocuments()</a> and <a href="#queryAttachments">queryAttachments()</a>)
 * @property {Boolean} [enableLowPrecisionOrderBy]           -         Allow order by with low precision (only for <a href="#queryDocuments">queryDocuments()</a>, <a href="#sortBy">sortBy()</a> and <a href="#sortByDescending">sortByDescending</a>)
 * @memberof Collection
 *
 */

/**
 * Callback to execute after completion of a request.
 * @callback RequestCallback
 * @param {Object} error                                     -         Will contain error information if an error occurs, undefined otherwise.
 * @param {ErrorCodes} error.number                          -         The HTTP response code corresponding to the error.
 * @param {string} error.body                                -         A string containing the error information.
 * @param {Object} resource                                  -         <p>An object that represents the requested resource (document or attachment).<br/>This is undefined if an error occurs in the operation.</p>
 * @param {Object} options                                   -         Information associated with the response to the operation.
 * @param {Boolean} options.statusCode                       -         HTTP response status code that describes the result of the operation, only for sucessful operations.
 * @param {string} options.currentCollectionSizeInMB         -         Comma delimited string containing the collection's current quota metrics (storage, number of stored procedure, triggers and UDFs) after completion of the operation.
 * @param {string} options.maxCollectionSizeInMB             -         Comma delimited string containing the collection's maximum quota metrics (storage, number of stored procedure, triggers and UDFs).
 * @param {Boolean} options.notModified                      -         Set to true if the requested resource has not been modified compared to the provided ETag in the ifNoneMatch parameter for a read request.
 * @param {Object}
 * @memberof Collection
 */

/**
 * The callback to execute after completion of read feed or query request.
 * @callback FeedCallback
 * @param {Object} error                                     -         Will contain error information if an error occurs, undefined otherwise.
 * @param {ErrorCodes} error.number                          -         The HTTP response code corresponding to the error.
 * @param {string} error.body                                -         A string containing the error information.
 * @param {Array} resources                                  -         <p>An array or resources (documents or attachments) that was read.<br/>This is undefined if an error occurs in the operation.</p>
 * @param {Object} options                                   -         Information associated with the response to the operation.
 * @param {string} options.continuation                      -         Opaque token for continuing the read feed or query.
 * @param {string} options.currentCollectionSizeInMB         -         Comma delimited string containing the collection's current quota metrics (storage, number of stored procedure, triggers and UDFs) after completion of the operation.
 * @param {string} options.maxCollectionSizeInMB             -         Comma delimited string containing the collection's maximum quota metrics (storage, number of stored procedure, triggers and UDFs).
 * @memberof Collection
 */

/**
 * The predicate function for a <a href="#filter">filter</a> query, which acts as a truth test of whether a document should be filtered or not.
 * @typedef {function} FilterPredicate
 * @param {Object} document                                  -         Input document.
 * @return {Boolean}                                         -         True if this document matches the filter, false otherwise. If true, this document will be added to the output result of the filter call.
 * @memberof Collection
 * @see The <a href="#filter">filter</a> call.
 * @example function(doc) { return doc.age < 30; }
 */

/**
 * The predicate function for a <a href="#map">map/projection</a>, <a href="#unwind">unwind/innerCollectionSelector</a>, which maps the input document's properties into a new document object.
 * @typedef {function} ProjectionPredicate
 * @param {Object} document                                  -         Input document.
 * @return {Object}                                          -         Output document, containing only the mapped properties. This output document will be added to the output result of the map call.
 * @memberof Collection
 * @see The <a href="#map">map</a> call.
 * @example function(doc) { return { name: doc.name, age: doc.age }; }
 */

/**
 * The predicate function for a <a href="#unwind">unwind/resultSelector</a>, which maps the input document's properties into a new document object.
 * @typedef {function} ResultSelectorPredicate
 * @param {Object} documentItem                           -         Input document or top level item from previous projection.
 * @param {Object} innerCollectionItem                    -         The item selected from inner collection.
 * @return {Object}                                       -         Output document, containing only the mapped properties. This output document will be added to the output result of the map call.
 * @memberof Collection
 * @see The <a href="#unwind">unwind</a> call.
 * @example function(customer, child) { return { customerName: customer.name, childName: child.name }; }
 */

/**
 * The predicate function for a <a href="#sortBy">sortBy</a> or a <a href="#sortByDescending">sortByDescending</a> query, which defines the property of the document to be used for sorting.
 * @typedef {function} SortByPredicate
 * @param {Object} document                                  -         Input document.
 * @return {String/Number}                                   -         A property of the document to use for sorting.
 * @memberof Collection
 * @see The <a href="#sortBy">sortBy</a> and <a href="#sortByDescending">sortByDescending</a> calls.
 * @example // Sort the documents by the 'name' property.
 function(doc){ return doc.name; }
 */

/**
 * <p>Object returned from a query function, namely <a href="#chain">chain</a>, <a href="#filter">filter</a>, <a href="#map">map</a>, <a href="#pluck">pluck</a>, <a href="#flatten">flatten</a>, or <a href="#value">value</a>.<br/>
 * If the query is part of a <a href="#chain">chained call</a>, then this object can be used to chain further queries until the final terminating <a href="#value">value</a> call.</p>
 * @typedef {Object} QueryResponse                           -         Object returned from a query function.
 * @property {Boolean} isAccepted                            -         True if the query has been queued, false if it is not queued because of a pending timeout.
 * @memberof Collection
 *
 */

/**  Gets the context object that can be used for executing operations on DocumentDB storage.
 *   @name getContext
 *   @function
 *   @global
 *   @returns {Context} Object that is used for executing operations on DocumentDB storage inside the JavaScript function.
 */

/**  The Context object provides access to all operations that can be performed on DocumentDB data, as well as access to the request and response objects.
 *   @name Context
 *   @class
 */

/**  <p>The Request object represents the request message that was sent to the server. This includes information about HTTP headers and the body of the HTTP request sent to the server.<br/>
 *   For triggers, the request represents the operation that is executing when the trigger is run. For example, if the trigger is being run ("triggered") on the creation of a document, then<br/>
 *   the request body contains the JSON body of the document to be created. This can be accessed through the request object and (as JSON) can be natively consumed in JavaScript.<br/>
 *   For stored procedures, the request contains information about the request sent to execute the stored procedure.</p>
 *   @name Request
 *   @class
 */

/**  <p>The Response object represents the response message that will be sent from the server in response to the requested operation. This includes information about the HTTP headers and body of the response from the server.<br/>
 *   The Response object is not present in pre-triggers because they are run before the response is generated.<br/>
 *   For post-triggers, the response represents the operation that was executed before the trigger. For example, if the post-trigger is being run ("triggered") after the creation of a document, then<br/>
 *   the response body contains the JSON body of the document that was created. This can be accessed through the response object and (as JSON) can be natively consumed in JavaScript.<br/>
 *   For stored procedures, the response can be manipulated to send output back to the client-side.<br/><br/>
 *   <b>Note</b>: this object not available in pre-triggers</p>
 *   @name Response
 *   @class
 */

/**  <p>Stored procedures and triggers are registered for a particular collection. The Collection object supports create, read, update and delete (CRUD) and query operations on documents and attachments in the current collection.<br/>
 *   All collection operations are completed asynchronously. You can provide a callback to handle the result of the operation, and to perform error handling if necessary.<br/>
 *   Stored procedures and triggers are executed in a time-limited manner. Long-running stored procedures and triggers are defensively timed out and all transactions performed are rolled back.<br/>
 *   We stop queuing collection operations if the stored procedure is close to timing out. You can inspect the boolean return value of all collection operations to see if an operation was not queued and handle this situation gracefully.</p>
 *   @name Collection
 *   @class
 */

/** Gets the request object.
 *   @name getRequest
 *   @function
 *   @instance
 *   @memberof Context
 *   @returns {Request} Object that provides access to the request message that was sent to the server.
 */

/**  <p>Gets the response object.<br/>
 *   <b>Note</b>: this is not available in pre-triggers.</p>
 *   @name getResponse
 *   @function
 *   @instance
 *   @memberof Context
 *   @returns {Response} Object that provides access to output through the response message to be sent from the server.
 */

/**  Gets the collection object.
 *   @name getCollection
 *   @function
 *   @instance
 *   @memberof Context
 *   @returns {Collection} Object that provides server-side access to DocumentDB database. It supports operations on documents and attachments in the collection.
 */

/**  Terminates the script and rolls back the transaction. The script is executed in the context of a transaction, which can be rolled back by using this method. This method is the only way to prevent script transaction from committing in promise callback. For non-promise scenarios, to abort the transaction, using unhandled exception is more recommemded than this.
 *   @name abort
 *   @function
 *   @instance
 *   @memberof Context
 *    @param {Object} err                                  -         The exception object to serve as the reason of the abort.
 *    @example getContext().abort(new Error('abort'));
 */

/** Gets the request body.
 *  @name getBody
 *  @function
 *  @instance
 *  @memberof Request
 *  @returns {string} The request body.
 */

/** <p>Sets the request body.<br>
 *  Note: this can be only used in a pre-trigger to overwrite the existing request body.<br />
 *  The overwritten request body will then be used in the operation associated with this pre-trigger.</p>
 *  @name setBody
 *  @function
 *  @instance
 *  @memberof Request
 *  @param {string} value - the value to set in the request body
 */

/** Gets a specified request header value.
 *  @name getValue
 *  @function
 *  @instance
 *  @memberof Request
 *  @param {string} key - the name of the header to retrieve
 *  @returns {string} The value of the requested header.
 */

/** <p>Sets a specified request header value.<br>
 *  Note: this method cannot be used to create new headers.</p>
 *  @name setValue
 *  @function
 *  @instance
 *  @memberof Request
 *  @param {string} key    - the name of the header
 *  @param {string} value  - the value of the header
 */

/** Gets the response body.
 *  @name getBody
 *  @function
 *  @instance
 *  @memberof Response
 *  @returns {string} The response body.
 */

/** <p>Sets the response body.<br />
 * Note: This cannot be done in pre-triggers.<br />
 * In post-triggers, the response body is already set with the requested resource and will be overwritten with this call.<br />
 * In stored procedures, this call can be used to set the response message body as output to the calling client.</p>
 * @name setBody
 * @function
 * @instance
 * @memberof Response
 * @param {string} value - the value to set in the response body
 */

/** Gets a specified response header value.
 * @name getValue
 * @function
 * @instance
 * @memberof Response
 * @param {string} key - the name of the header to retrieve
 * @returns {string} The value of the response header.
 */

/** <p>Sets a specified response header value.<br />
 * Note: this method cannot be used to create new headers.</p>
 * @name setValue
 * @function
 * @instance
 * @memberof Response
 * @param {string} key    - the name of the header
 * @param {string} value  - the value of the header
 */

/** <p>Gets the OperationType for the request with a pre-trigger or post-trigger<br />
 * @name getOperationType
 * @function
 * @instance
 * @memberof Request
 * @returns {Create/Replace/Upsert/Delete} The value of the operation type corresponding to the current request.
 */

/** <p>Gets a current quota usage for the resource associated with a post-trigger<br />
 * Note: this method is only available in post-triggers</p>
 * @name getResourceQuotaCurrentUsage
 * @function
 * @instance
 * @memberof Response
 * @returns {string} The value of the current quota usage.
 */

/** <p>Gets a maximum quota allowed for the resource associated with a post-trigger<br />
 * Note: this method is only available in post-triggers</p>
 * @name getMaxResourceQuota
 * @function
 * @instance
 * @memberof Response
 * @returns {string} The value of the maximum allowed quota usage.
 */

/** <p>Adds log details to the response header x-ms-documentdb-script-log-results.<br />
 * Note: this method is only available for stored procedures and must be enabled by setting the request header x-ms-script-enable-logging to true.</p>
 * @name console.log
 * @function
 * @param {string} logStr - the string to be added to the response header
 * @param {string} [, argument, ...] - optional string arguments that are substituted for corresponding instances of %s in logStr
 */
