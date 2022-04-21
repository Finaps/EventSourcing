/**
 * A Cosmos DB stored procedure that bulk deletes documents for a given partitionId and aggregateId.<br/>
 * Note: You may need to execute this sproc multiple times (depending whether the sproc is able to delete every document within the execution timeout limit).
 *
 * @function
 * @param {string} containerId - The id of the container where the aggregate exists
 * @param {string} partitionId - The partition id of the aggregate that is to be deleted
 * @param {string} aggregateId - The aggregate id of the aggregate that is to be deleted
 * @param {int} recordKind - The integer corresponding to the RecordKind value 'Event'
 * @returns {Object.<number, boolean>} Returns an object with the two properties:<br/>
 *   deleted - contains a count of documents deleted<br/>
 *   continuation - a boolean whether you should execute the sproc again (true if there are more documents to delete; false otherwise).
 */
function deleteAllEvents(containerId, partitionId, aggregateId, recordKind) {
    var collection = getContext().getCollection();
    var collectionLink = collection.getSelfLink();
    var response = getContext().getResponse();
    var responseBody = {
        deleted: 0,
        continuation: true
    };

    // Validate input.
    if(!containerId) throw new Error('Invalid container id');
    if(!partitionId) throw new Error('Invalid partition id');
    if(!aggregateId) throw new Error('Invalid aggregate id');
    if(!recordKind) throw new Error('Invalid record kind');

    var query = `SELECT * FROM ${containerId} e WHERE e.PartitionId = '${partitionId}' AND e.AggregateId = '${aggregateId}' AND e.Kind = ${recordKind}`;

    // Get the current aggregate version -> Create reservation event -> Delete reservation event -> Query and delete all documents related to the aggregate
    getIndex();

    // Find the current version of the aggregate by getting the maximal Index of the events
    function getIndex() {
        var versionQuery = `SELECT Max(e.Index) AS Index FROM ${containerId} e WHERE e.PartitionId = '${partitionId}' AND e.AggregateId = '${aggregateId}' AND e.Kind = ${recordKind}`;
        var isAccepted = collection.queryDocuments(collectionLink, versionQuery, {}, createReservation);
    }

    // Create reservation event to prevent concurrency issues when deleting
    function createReservation(err, retrievedDocs, responseOptions) {
        if (err) throw err;
        var index = retrievedDocs[0].Index >= 0 ? retrievedDocs[0].Index + 1 : 0;
        var isAccepted = collection.createDocument(
            collectionLink,
            {
                id: `Event|${aggregateId}[${index}]`,
                PartitionId: partitionId,
                AggregateId: aggregateId,
                Index: index,
                Kind: recordKind},
            {},
            deleteReservation);
    }

    // Delete reservation
    function deleteReservation(err, resource, responseOptions) {
        if (err) throw err;
        var isAccepted = collection.deleteDocument(resource._self, {}, (err, resource, responseOptions) => {
            if (err) throw err;
            // Continue with querying and deleting
            tryQueryAndDelete();
        });
    }

    // Recursively runs the query w/ support for continuation tokens.
    // Calls tryDelete(documents) as soon as the query returns documents.
    function tryQueryAndDelete(continuation) {
        var requestOptions = {continuation: continuation};

        var isAccepted = collection.queryDocuments(collectionLink, query, requestOptions,  (err, retrievedDocs, responseOptions) => {
            if (err) throw err;

            if (retrievedDocs.length > 0) {
                // Begin deleting documents as soon as documents are returned form the query results.
                // tryDelete() resumes querying after deleting; no need to page through continuation tokens.
                //  - this is to prioritize writes over reads given timeout constraints.
                tryDelete(retrievedDocs);
            } else if (responseOptions.continuation) {
                // Else if the query came back empty, but with a continuation token; repeat the query w/ the token.
                tryQueryAndDelete(responseOptions.continuation);
            } else {
                // Else if there are no more documents and no continuation token - we are finished deleting documents.
                responseBody.continuation = false;
                response.setBody(responseBody);
            }
        });

        // If we hit execution bounds - return continuation: true.
        if (!isAccepted) {
            response.setBody(responseBody);
        }
    }

    // Recursively deletes documents passed in as an array argument.
    // Attempts to query for more on empty array.
    function tryDelete(documents) {
        if (documents.length > 0) {
            // Delete the first document in the array.
            var isAccepted = collection.deleteDocument(documents[0]._self, {},(err, responseOptions) => {
                if (err) throw err;

                responseBody.deleted++;
                documents.shift();
                // Delete the next document in the array.
                tryDelete(documents);
            });

            // If we hit execution bounds - return continuation: true.
            if (!isAccepted) {
                response.setBody(responseBody);
            }
        } else {
            // If the document array is empty, query for more documents.
            tryQueryAndDelete();
        }
    }
}
    
    