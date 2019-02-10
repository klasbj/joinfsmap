import * as React from 'react';

interface IFetcherProps<T> {
    src: string,
    children: (data: T | null, loading: boolean, error: string | null) => JSX.Element[] | JSX.Element
}

interface IFetcherState<T> {
    data: T | null,
    isLoading: boolean,
    error: string | null
}

export class Fetcher<T> extends React.Component<IFetcherProps<T>, IFetcherState<T>> {
    constructor(props: IFetcherProps<T>) {
        super(props);
        this.state = {
            data: null,
            error: null,
            isLoading: true
        }
    }

    public componentDidMount() {
        this.fetchData();
    }

    public render() {
        return this.props.children(this.state.data, this.state.isLoading, this.state.error);
    }

    private fetchData() {
        this.setState({ isLoading: true });
        fetch(this.props.src)
            .then(r => {
                if (r.ok) {
                    return r.json();
                }

                throw Error(`${r.statusText} (${r.status})`);
            })
            .then(r => {
                this.setState({
                    data: r,
                    error: null,
                    isLoading: false
                });
            })
            .catch(r => {
                let reason: string;
                const e = r as Error;
                if (e) {
                    reason = e.message;
                } else {
                    reason = r as string;
                }

                this.setState({
                    error: reason || "Unknown error",
                    isLoading: false
                });
            });
    }
}
